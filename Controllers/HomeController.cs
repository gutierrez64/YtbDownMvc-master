using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Work.Models;
using Work.Util;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Work.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly YoutubeClient _youtube;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _youtube = new YoutubeClient();
        }

        // Retorna a página inicial
        public IActionResult Index()
        {
            return View();
        }

        // Retorna a página de privacidade
        public IActionResult Privacy()
        {
            return View();
        }

        // Ação para lidar com erros
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Ação para processar o formulário de download
        [HttpPost]
        public async Task<IActionResult> Baixar(string videoUrl, CaminhoDownload request)
        {
            try
            {
                // Obter informações do vídeo a partir da URL
                var videoInformacao = await _youtube.Videos.GetAsync(videoUrl);
                var videoTitulo = videoInformacao.Title;

                // Definir diretório de saída com base na opção escolhida (vídeo ou áudio)
                string diretorioDeSaida = GetDownloadDirectory(request.DownloadDirectory);

                // Montar caminho completo do arquivo de saída
                string diretorioSaida = GetOutputFilePath(diretorioDeSaida, videoTitulo, request.DownloadDirectory);

                // Criar diretório de saída se não existir
                Directory.CreateDirectory(diretorioDeSaida);

                // Obter informações do fluxo de mídia a ser baixado
                IStreamInfo infoFluxo = GetStreamInfo(videoUrl, request.DownloadDirectory);

                // Verificar se existe uma stream de mídia disponível
                if (infoFluxo != null)
                {
                    // Baixar o fluxo de mídia para o arquivo de saída
                    await _youtube.Videos.Streams.DownloadAsync(infoFluxo, diretorioSaida);
                }
                else
                {
                    // Lançar exceção se não houver stream de mídia disponível
                    throw new Exception(request.DownloadDirectory == "BaixarVideo" ? "Nenhuma stream de vídeo disponível." : "Nenhuma stream de áudio disponível.");
                }

                // Determinar o tipo de conteúdo apropriado para a resposta
                string contentType = request.DownloadDirectory == "BaixarVideo" ? "video/mp4" : "audio/mpeg";

                // Retornar o arquivo físico como resultado da ação
                return PhysicalFile(diretorioSaida, contentType, Path.GetFileName(diretorioSaida));
            }
            catch (Exception ex)
            {
                // Lidar com exceções e retornar resposta de erro
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // Método auxiliar para obter o diretório de download com base na opção escolhida
        private string GetDownloadDirectory(string downloadDirectory)
        {
            // Escolher os diretórios desejados para vídeos e áudios
            return downloadDirectory == "BaixarVideo" ? "/home/gabriel/Desktop/videos" : "/home/gabriel/Desktop/videos";
        }

        // Método auxiliar para montar o caminho completo do arquivo de saída
        private string GetOutputFilePath(string diretorioDeSaida, string videoTitulo, string downloadDirectory)
        {
            // Formatar o nome do arquivo e determinar a extensão com base na opção escolhida
            videoTitulo = FormNome.FormatarNomeArquivo(videoTitulo, 100);
            string fileExtension = downloadDirectory == "BaixarVideo" ? ".mp4" : ".mp3";
            return Path.Combine(diretorioDeSaida, $"{videoTitulo}{fileExtension}");
        }

        // Método auxiliar para obter informações do fluxo de mídia
        private IStreamInfo GetStreamInfo(string videoUrl, string downloadDirectory)
        {
            // Obter informações do manifesto de streams assincronamente
            var informacaoUrl = _youtube.Videos.Streams.GetManifestAsync(videoUrl).ConfigureAwait(false).GetAwaiter().GetResult();

            // Escolher o tipo apropriado de fluxo de acordo com a opção escolhida
            return downloadDirectory == "BaixarVideo" ? informacaoUrl.GetMuxedStreams().GetWithHighestVideoQuality() : informacaoUrl.GetAudioOnlyStreams().GetWithHighestBitrate();
        }
    }
}
