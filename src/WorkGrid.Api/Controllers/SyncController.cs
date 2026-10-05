using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkGrid.Infrastructure.Remote.Sync;

namespace WorkGrid.Api.Controllers;

[ApiController]
[Route("api/sync")]
[Authorize]
public sealed class SyncController : ControllerBase
{
    private readonly Sync.RelayCoordinator _coordinator;

    public SyncController(Sync.RelayCoordinator coordinator)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    }

    [HttpPost("session")]
    public IActionResult CreateSession([FromBody] SyncSessionRequest request)
    {
        if (request.DeviceId == Guid.Empty)
            return BadRequest("DeviceId cannot be empty.");

        var response = new SyncSessionResponse(
            SessionId: Guid.NewGuid(),
            DeviceId: request.DeviceId,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(2)
        );

        return Ok(response);
    }

    [HttpPost("manifest")]
    public IActionResult PostManifest([FromBody] SyncManifestEnvelope envelope)
    {
        if (envelope.SenderDeviceId == Guid.Empty || envelope.Manifest is null)
            return BadRequest("Invalid manifest payload.");

        _coordinator.StoreManifest(envelope.SenderDeviceId, envelope.Manifest);
        return Ok();
    }

    [HttpGet("manifest/{deviceId:guid}")]
    public IActionResult GetManifest(Guid deviceId)
    {
        var manifest = _coordinator.GetManifest(deviceId);
        if (manifest is null)
            return NotFound($"No manifest found for device {deviceId}");

        return Ok(manifest);
    }

    [HttpPost("envelope")]
    public IActionResult PushEnvelope([FromBody] SyncEnvelope envelope)
    {
        if (envelope.EnvelopeId == Guid.Empty || envelope.SenderDeviceId == Guid.Empty || envelope.RecipientDeviceId == Guid.Empty)
            return BadRequest("Invalid envelope parameters.");

        _coordinator.EnqueueChanges(envelope);
        return Accepted();
    }

    [HttpGet("pending/{deviceId:guid}")]
    public IActionResult PollPending(Guid deviceId)
    {
        var pending = _coordinator.PollPending(deviceId);
        return Ok(pending);
    }

    [HttpPost("ack")]
    public IActionResult Acknowledge([FromBody] SyncAckRequest ack)
    {
        _coordinator.Acknowledge(ack.DeviceId, ack.EnvelopeId);
        return Ok();
    }
}
