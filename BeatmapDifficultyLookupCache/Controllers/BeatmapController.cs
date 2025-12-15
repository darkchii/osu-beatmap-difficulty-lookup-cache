using BeatmapDifficultyLookupCache.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace BeatmapDifficultyLookupCache.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BeatmapController : Controller
    {
        private readonly DifficultyCache cache;

        public BeatmapController(DifficultyCache cache)
        {
            this.cache = cache;
        }

        [HttpPost] //returns a stream of the beatmap file corresponding to the given beatmap ID
        public async Task<FileStreamResult> Post([FromBody] BeatmapRequest request)
        {
            var beatmapStream = await cache.getBeatmapStream(request.BeatmapId);
            return new FileStreamResult(beatmapStream, "application/octet-stream")
            {
                FileDownloadName = $"{request.BeatmapId}.osu"
            };
        }
    }
}
