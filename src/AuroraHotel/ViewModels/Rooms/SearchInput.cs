using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public class SearchInput
{
    [DataType(DataType.Date)]
    public DateOnly? ArrivalDate { get; set; }

    [DataType(DataType.Date)]
    public DateOnly? DepartureDate { get; set; }

    [Range(1, 20, ErrorMessage = "{0} nằm ngoài phạm vi cho phép.")]
    public int GuestCount { get; set; } = 2;
    public int? RoomTypeId { get; set; }

    [Range(0, 1000000000, ErrorMessage = "{0} nằm ngoài phạm vi cho phép.")]
    public decimal? MinPrice { get; set; }

    [Range(0, 1000000000, ErrorMessage = "{0} nằm ngoài phạm vi cho phép.")]
    public decimal? MaxPrice { get; set; }
    public string Sort { get; set; } = "price";

    [Range(1, 100000, ErrorMessage = "{0} nằm ngoài phạm vi cho phép.")]
    public int Page { get; set; } = 1;
}
