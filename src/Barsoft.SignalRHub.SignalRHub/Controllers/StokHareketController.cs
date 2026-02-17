using Barsoft.SignalRHub.Application.DTOs;
using Barsoft.SignalRHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Barsoft.SignalRHub.SignalRHub.Controllers;

/// <summary>
/// Stok hareket verilerini sorgulama endpoint'leri
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StokHareketController : ControllerBase
{
    private readonly IStokHareketRepository _repository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<StokHareketController> _logger;

    public StokHareketController(
        IStokHareketRepository repository,
        IUserRepository userRepository,
        ILogger<StokHareketController> logger)
    {
        _repository = repository;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// En son N adet stok hareketini getirir
    /// Multi-tenant: Kullanıcının erişebildiği şubelere göre filtrelenir
    /// </summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(List<StokHareketDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StokHareketDto>>> GetRecent(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        // Get user ID from JWT
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        // Get user to get SubeIds for multi-tenant filtering
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Unauthorized();
        }

        // Parse SubeIds
        var subeIds = UserDto.ParseSubeIds(user.SubeIds);

        // Get recent records
        var records = await _repository.GetRecentAsync(count, subeIds, cancellationToken);

        // Map to DTOs
        var dtos = records.Select(x => new StokHareketDto
        {
            Id = x.Id,
            StokId = x.StokId,
            BelgeKodu = x.BelgeKodu,
            BelgeTarihi = x.BelgeTarihi,
            Miktar = x.Miktar,
            BirimFiyati = x.BirimFiyati,
            ToplamTutar = x.ToplamTutar,
            KdvTutari = x.KdvTutari,
            Aciklama = x.Aciklama ?? string.Empty,
            CreateDate = x.CreateDate,
            ChangeDate = x.ChangeDate,
            MasrafMerkeziId = x.MasrafMerkeziId,
            DepoId = x.DepoId
        }).ToList();

        _logger.LogInformation("User {UserCode} fetched {Count} recent stock movements", user.UserCode, dtos.Count);

        return Ok(dtos);
    }

    /// <summary>
    /// ID'ye göre tek bir stok hareketi getirir
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(StokHareketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StokHareketDto>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var record = await _repository.GetByIdAsync(id, cancellationToken);
        
        if (record == null)
        {
            return NotFound();
        }

        var dto = new StokHareketDto
        {
            Id = record.Id,
            StokId = record.StokId,
            BelgeKodu = record.BelgeKodu,
            BelgeTarihi = record.BelgeTarihi,
            Miktar = record.Miktar,
            BirimFiyati = record.BirimFiyati,
            ToplamTutar = record.ToplamTutar,
            KdvTutari = record.KdvTutari,
            Aciklama = record.Aciklama ?? string.Empty,
            CreateDate = record.CreateDate,
            ChangeDate = record.ChangeDate,
            MasrafMerkeziId = record.MasrafMerkeziId,
            DepoId = record.DepoId
        };

        return Ok(dto);
    }
}
