namespace BookingSystem.Domain.Entities;

public class Slot
{
    public int Id { get; set; }
    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
