using Microsoft.AspNetCore.Mvc;
using TournamentApp.Models;
using TournamentApp.Services;

namespace TournamentApp.Controllers
{
    public class TournamentsController : Controller
    {
        private readonly ITournamentService _tournamentService;
        
        public TournamentsController(ITournamentService tournamentService)
        {
            _tournamentService = tournamentService;
        }
        
        public async Task<IActionResult> Index()
        {
            var tournaments = await _tournamentService.GetAllTournamentsAsync();
            return View(tournaments);
        }
        
        public async Task<IActionResult> Create()
        {
            var participants = await _tournamentService.GetAllParticipantsAsync();
            ViewBag.Participants = participants;
            return View(new Tournament());
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tournament tournament, List<int> participantIds)
        {
            if (ModelState.IsValid)
            {
                if (participantIds == null || participantIds.Count < 3 || participantIds.Count > 5)
                {
                    ModelState.AddModelError("", "Выберите от 3 до 5 участников");
                    var participants = await _tournamentService.GetAllParticipantsAsync();
                    ViewBag.Participants = participants;
                    return View(tournament);
                }
                
                try
                {
                    await _tournamentService.CreateTournamentAsync(tournament, participantIds);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Ошибка при создании турнира: " + ex.Message);
                }
            }
            
            var allParticipants = await _tournamentService.GetAllParticipantsAsync();
            ViewBag.Participants = allParticipants;
            return View(tournament);
        }
        
        public async Task<IActionResult> Details(int id)
        {
            var tournament = await _tournamentService.GetTournamentByIdAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }
            
            return View(tournament);
        }
        
        public async Task<IActionResult> Standings(int id)
        {
            var tournament = await _tournamentService.GetTournamentByIdAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }
            
            var standings = await _tournamentService.GetTournamentStandingsAsync(id);
            ViewBag.Tournament = tournament;
            ViewBag.Standings = standings;
            return View();
        }
        
        public async Task<IActionResult> Matches(int id)
        {
            var matches = await _tournamentService.GetTournamentMatchesAsync(id);
            var tournament = await _tournamentService.GetTournamentByIdAsync(id);
            
            if (tournament == null)
            {
                return NotFound();
            }
            
            ViewBag.Tournament = tournament;
            return View(matches);
        }
        
        public async Task<IActionResult> Edit(int id)
        {
            var tournament = await _tournamentService.GetTournamentByIdAsync(id);
            if (tournament == null)
            {
                return NotFound();
            }
            
            var participants = await _tournamentService.GetAllParticipantsAsync();
            ViewBag.Participants = participants;
            return View(tournament);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tournament tournament)
        {
            if (id != tournament.Id)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                var success = await _tournamentService.UpdateTournamentAsync(id, tournament);
                if (success)
                {
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    return NotFound();
                }
            }
            
            var participants = await _tournamentService.GetAllParticipantsAsync();
            ViewBag.Participants = participants;
            return View(tournament);
        }
        
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var tournament = await _tournamentService.GetTournamentByIdAsync(id);
                if (tournament == null)
                {

                    TempData["Error"] = $"Турнир с ID {id} не найден в базе данных.";
                    return RedirectToAction(nameof(Index));
                }

                return View(tournament);
            }
            catch (Exception ex)
            {

                TempData["Error"] = $"Ошибка при получении турнира: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var success = await _tournamentService.DeleteTournamentAsync(id);
                if (!success)
                {
                    TempData["Error"] = $"Не удалось удалить турнир с ID {id}. Возможно, он уже был удален.";
                    return RedirectToAction(nameof(Index));
                }
                
                TempData["Success"] = "Турнир успешно удален!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка при удалении турнира: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneratePlayoff(int id)
        {
            var success = await _tournamentService.GeneratePlayoffAsync(id);
            if (!success)
            {
                TempData["Error"] = "Не удалось сгенерировать плей-офф. Возможно, он уже был создан.";
            }
            else
            {
                TempData["Success"] = "Плей-офф успешно сгенерирован!";
            }
            
            return RedirectToAction(nameof(Matches), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateFinal(int id)
        {
            var success = await _tournamentService.GenerateFinalAsync(id);
            if (!success)
            {
                TempData["Error"] = "Не удалось создать финальный матч. Возможно, он уже был создан.";
            }
            else
            {
                TempData["Success"] = "Финальный матч успешно создан!";
            }
            
            return RedirectToAction(nameof(Matches), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRandomGroupResults(int id)
        {
            var success = await _tournamentService.GenerateRandomGroupResultsAsync(id);
            if (!success)
            {
                TempData["Error"] = "Не удалось сгенерировать случайные результаты. Возможно, все матчи уже завершены.";
            }
            else
            {
                TempData["Success"] = "Случайные результаты для групповых матчей успешно сгенерированы!";
            }
            
            return RedirectToAction(nameof(Matches), new { id });
        }


    }
} 