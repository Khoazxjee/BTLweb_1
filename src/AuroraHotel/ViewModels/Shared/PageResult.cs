using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public record PageResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize = 9)
{
    public int Pages => Math.Max(1, (int)Math.Ceiling((double)Total / PageSize));
}
