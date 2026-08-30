namespace BookingSystem.Domain.Entities;

public class Booking
{
    public int Id { get; set; }

    public int SlotId { get; set; }
    public Slot Slot { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
