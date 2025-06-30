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
        
        public async Task<IActionResult> Edit(int id)
        {
            var participant = await _tournamentService.GetParticipantByIdAsync(id);
            if (participant == null)
            {
                return NotFound();
            }
            return View(participant);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Participant participant)
        {
            if (id != participant.Id)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                var success = await _tournamentService.UpdateParticipantAsync(id, participant);
                if (success)
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return NotFound();
                }
            }
            return View(participant);
        }
        
        public async Task<IActionResult> Delete(int id)
        {
            var participant = await _tournamentService.GetParticipantByIdAsync(id);
            if (participant == null)
            {
                return NotFound();
            }
            return View(participant);
        }
        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _tournamentService.DeleteParticipantAsync(id);
            if (!success)
            {
                TempData["Error"] = "Невозможно удалить участника с завершенными матчами";
                return RedirectToAction(nameof(Index));
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        public async Task<IActionResult> Statistics(int id)
        {
            var stats = await _tournamentService.GetParticipantStatisticsAsync(id);
            var participant = await _tournamentService.GetParticipantByIdAsync(id);
            
            if (participant == null)
            {
                return NotFound();
            }
            
            ViewBag.Participant = participant;
            return View(stats);
        }
    }
} 