namespace BookingSystem.Domain.Entities;

public class Resource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Slot> Slots { get; set; } = new List<Slot>();
}
