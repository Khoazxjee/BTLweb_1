using System.ComponentModel.DataAnnotations;
using AuroraHotel.Models;

namespace AuroraHotel.ViewModels;

public record SearchPage(SearchInput Search, PageResult<Room> Results, List<RoomType> Types);
