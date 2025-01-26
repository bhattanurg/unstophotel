using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

public class HomeController : Controller
{
    private static List<Room> rooms;

    public HomeController()
    {
        // Initialize rooms only once
        if (rooms == null)
        {
            rooms = new List<Room>();
            for (int floor = 1; floor <= 10; floor++)
            {
                int roomCount = (floor == 10) ? 7 : 10;
                for (int i = 1; i <= roomCount; i++)
                {
                    int roomNumber = floor * 100 + i;
                    rooms.Add(new Room
                    {
                        RoomNumber = roomNumber,
                        Floor = floor,
                        IsBooked = false
                    });
                }
            }
        }
    }

    public ActionResult Index()
    {
        return View(rooms);
    }

    [HttpPost]
    public ActionResult BookRooms(int roomsRequired)
    {
        if (roomsRequired < 1 || roomsRequired > 5)
        {
            ViewBag.Message = "You can book between 1 and 5 rooms.";
            return View("Index", rooms);
        }

        var availableRooms = rooms.Where(r => !r.IsBooked).ToList();
        var groupedByFloor = availableRooms.GroupBy(r => r.Floor).OrderBy(g => g.Key);

        List<Room> bookedRooms = null;

        foreach (var group in groupedByFloor)
        {
            if (group.Count() >= roomsRequired)
            {
                bookedRooms = group.Take(roomsRequired).ToList();
                break;
            }
        }

        if (bookedRooms == null)
        {
            ViewBag.Message = "Not enough rooms available.";
            return View("Index", rooms);
        }

        foreach (var room in bookedRooms)
        {
            room.IsBooked = true;
        }

        ViewBag.Message = $"Successfully booked {bookedRooms.Count} rooms.";
        return View("Index", rooms);
    }

    [HttpPost]
    public ActionResult Reset()
    {
        foreach (var room in rooms)
        {
            room.IsBooked = false;
        }

        ViewBag.Message = "All bookings have been reset.";
        return View("Index", rooms);
    }

    [HttpPost]
    public ActionResult RandomizeOccupancy()
    {
        var random = new Random();
        foreach (var room in rooms)
        {
            room.IsBooked = random.Next(0, 2) == 1;
        }

        ViewBag.Message = "Random room occupancy generated.";
        return View("Index", rooms);
    }
}
