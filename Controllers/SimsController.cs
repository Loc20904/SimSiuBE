using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViettalAPI.Data;
using ViettalAPI.Models;

namespace ViettalAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SimsController : ControllerBase
    {
        private readonly ViettalDbContext _context;

        public SimsController(ViettalDbContext context)
        {
            _context = context;
        }

        // GET: api/sims
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BeautifulSim>>> GetSims()
        {
            return await _context.Sims.ToListAsync();
        }

        // GET: api/sims/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BeautifulSim>> GetSim(string id)
        {
            var sim = await _context.Sims.FindAsync(id);

            if (sim == null)
            {
                return NotFound(new { message = $"Không tìm thấy SIM với ID: {id}" });
            }

            return sim;
        }

        // POST: api/sims
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<BeautifulSim>> PostSim([FromBody] BeautifulSim sim)
        {
            if (sim == null)
            {
                return BadRequest(new { message = "Dữ liệu SIM không hợp lệ." });
            }

            // Check if ID is provided, if not generate one
            if (string.IsNullOrWhiteSpace(sim.Id))
            {
                sim.Id = "sim-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            var exists = await _context.Sims.AnyAsync(s => s.Id == sim.Id);
            if (exists)
            {
                return Conflict(new { message = "Mã SIM này đã tồn tại." });
            }

            _context.Sims.Add(sim);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSim), new { id = sim.Id }, sim);
        }

        // PUT: api/sims/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PutSim(string id, [FromBody] BeautifulSim sim)
        {
            if (id != sim.Id)
            {
                return BadRequest(new { message = "Mã SIM trong URL và body không khớp." });
            }

            var existingSim = await _context.Sims.FindAsync(id);
            if (existingSim == null)
            {
                return NotFound(new { message = $"Không tìm thấy SIM với ID: {id}" });
            }

            // Update fields
            existingSim.PhoneNumber = sim.PhoneNumber;
            existingSim.Carrier = sim.Carrier;
            existingSim.Type = sim.Type;
            existingSim.Price = sim.Price;
            existingSim.Meaning = sim.Meaning;
            existingSim.Status = sim.Status;
            existingSim.Description = sim.Description;

            _context.Entry(existingSim).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SimExists(id))
                {
                    return NotFound(new { message = $"Không tìm thấy SIM với ID: {id}" });
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/sims/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSim(string id)
        {
            var sim = await _context.Sims.FindAsync(id);
            if (sim == null)
            {
                return NotFound(new { message = $"Không tìm thấy SIM với ID: {id}" });
            }

            _context.Sims.Remove(sim);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<bool> SimExists(string id)
        {
            return await _context.Sims.AnyAsync(e => e.Id == id);
        }
    }
}
