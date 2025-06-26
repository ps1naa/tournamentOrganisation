using Microsoft.AspNetCore.Mvc;
using TournamentApp.Services;

namespace TournamentApp.Controllers
{
    public class MatchesController : Controller
    {
        private readonly ITournamentService _tournamentService;
        
        public MatchesController(ITournamentService tournamentService)
        {
            _tournamentService = tournamentService;
        }
        
        public async Task<IActionResult> Edit(int id)
        {
            var match = await _tournamentService.GetMatchByIdAsync(id);
            if (match == null)
            {
                return NotFound();
            }
            
            ViewBag.Match = match;
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int? homeScore, int? awayScore, string isCompleted)
        {
            bool isCompletedBool = !string.IsNullOrEmpty(isCompleted) && isCompleted == "true";
            
            var match = await _tournamentService.GetMatchByIdAsync(id);
            if (match == null)
            {
                return NotFound();
            }
            
            var tournamentId = match.TournamentId;
            
            var success = await _tournamentService.UpdateMatchResultAsync(id, homeScore, awayScore, isCompletedBool);
            if (!success)
            {
                return NotFound();
            }
            
            return RedirectToAction("Matches", "Tournaments", new { id = tournamentId });
        }
    }
} 