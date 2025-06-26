using Microsoft.AspNetCore.Mvc;
using TournamentApp.Models;
using TournamentApp.Services;

namespace TournamentApp.Controllers
{
    public class ParticipantsController : Controller
    {
        private readonly ITournamentService _tournamentService;
        
        public ParticipantsController(ITournamentService tournamentService)
        {
            _tournamentService = tournamentService;
        }
        
        public async Task<IActionResult> Index()
        {
            var participants = await _tournamentService.GetAllParticipantsAsync();
            return View(participants);
        }
        
        public IActionResult Create()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Participant participant)
        {
            if (ModelState.IsValid)
            {
                await _tournamentService.CreateParticipantAsync(participant);
                return RedirectToAction(nameof(Index));
            }
            return View(participant);
        }
    }
} 