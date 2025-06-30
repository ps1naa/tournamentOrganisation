using Microsoft.AspNetCore.Mvc;
using TournamentApp.Services;

namespace TournamentApp.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly ITournamentService _tournamentService;
        
        public StatisticsController(ITournamentService tournamentService)
        {
            _tournamentService = tournamentService;
        }
        
        public async Task<IActionResult> Index()
        {
            var participants = await _tournamentService.GetAllParticipantsAsync();
            var participantStats = new List<Models.ParticipantStatistics>();
            
            foreach (var participant in participants)
            {
                var stats = await _tournamentService.GetParticipantStatisticsAsync(participant.Id);
                participantStats.Add(stats);
            }
            
            ViewBag.ParticipantStatistics = participantStats;
            
            var headToHeadStats = await _tournamentService.GetHeadToHeadStatisticsAsync();
            ViewBag.HeadToHeadStatistics = headToHeadStats;
            
            return View();
        }
        
        public async Task<IActionResult> Participant(int id)
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
        
        public async Task<IActionResult> HeadToHead()
        {
            var headToHeadStats = await _tournamentService.GetHeadToHeadStatisticsAsync();
            return View(headToHeadStats);
        }
    }
} 