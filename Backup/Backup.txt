using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using Work.Util;
using YoutubeExplode.Videos.Streams;
using System.Net;

namespace Work.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaixarController : ControllerBase
    {
        // ClasseYouTUbeClient que irá fornecer métodos para interagir com a API do YouTube.
        private readonly YoutubeClient _youtube;

        // COnstrutor
        public BaixarController()
        {
            _youtube = new YoutubeClient();
        }

        [HttpPost]
        public async Task<IActionResult> BaixarVideo([FromBody] CaminhoDownload request)
        {
            try
            {
                var videoInformacao = await _youtube.Videos.GetAsync(request.VideoUrl);
                var videoTitulo = videoInformacao.Title;

                videoTitulo = FormNome.FormatarNomeArquivo(videoTitulo, 100);

                var diretorioDeSaida = request.DownloadDirectory;
                var diretorioSaida = Path.Combine(diretorioDeSaida, $"{videoTitulo}.mp4");

                Directory.CreateDirectory(diretorioDeSaida);

                var informacaoUrl = await _youtube.Videos.Streams.GetManifestAsync(request.VideoUrl);
                var infoFLuxo = informacaoUrl.GetMuxedStreams().GetWithHighestVideoQuality();

                if (infoFLuxo != null)
                {
                    await _youtube.Videos.Streams.DownloadAsync(infoFLuxo, diretorioSaida);
                }
                else
                {
                    throw new Exception("Nenhuma stream de vídeo disponível.");
                }

                return PhysicalFile(diretorioSaida, "video/mp4");
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        [HttpPost("BaixarAudio")]
        public async Task<IActionResult> BaixarAudio([FromBody] CaminhoDownload requst)
        {
            try
            {
                var videoInformacao = await _youtube.Videos.GetAsync(requst.VideoUrl);
                var videoTitulo = videoInformacao.Title;

                var caracteresInvalidos = Path.GetInvalidFileNameChars();
                foreach(char c in caracteresInvalidos)
                {
                    videoTitulo = videoTitulo.Replace(c,'_');
                }

               const int limiteTamanhoTitulo = 100; 

                if (videoTitulo.Length > limiteTamanhoTitulo)
                {
                    videoTitulo = videoTitulo.Substring(0, limiteTamanhoTitulo);
                }
                

                var diretorioDeSaida = requst.DownloadDirectory;
                var diretorioSaida = Path.Combine(diretorioDeSaida, $"{videoTitulo}.mp3");

                Directory.CreateDirectory(diretorioDeSaida);

                var informacaoUrl = await _youtube.Videos.Streams.GetManifestAsync(requst.VideoUrl);
                var infoFluxoAudio = informacaoUrl.GetAudioOnlyStreams().GetWithHighestBitrate();

                if (infoFluxoAudio != null)
                {
                    await _youtube.Videos.Streams.DownloadAsync(infoFluxoAudio, diretorioSaida);
                }
                else
                {
                    throw new Exception("Nenhuma stream de áudio disponível.");
                }

                return PhysicalFile(diretorioSaida, "audio/mpeg");

            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
