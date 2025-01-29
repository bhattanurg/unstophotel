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
        var groupedByFloor = availableRooms.GroupBy(r => r.Floor).OrderBy(g => g.Key).ToList();

        List<Room> bookedRooms = new List<Room>();

        // Priority 1: Check if rooms are available on the same floor all together next to each other
        foreach (var group in groupedByFloor)
        {
            if (group.Count() >= roomsRequired)
            {
                var roomsOnFloor = group.OrderBy(r => r.RoomNumber).ToList();
                var adjacentRooms = FindAdjacentRooms(roomsOnFloor, roomsRequired);

                if (adjacentRooms != null)
                {
                    bookedRooms = adjacentRooms;
                    break;
                }
            }
        }

        // Priority 2: If no adjacent rooms, find the combination with the minimum travel time across all floors
        if (bookedRooms.Count == 0)
        {
            bookedRooms = FindBestCombinationAcrossFloors(availableRooms, roomsRequired);
        }

        // Priority 3: If not enough rooms on one floor, book across floors with minimum travel time
        if (bookedRooms.Count == 0)
        {
            bookedRooms = GetRoomsAcrossFloors(availableRooms, roomsRequired);
        }

        if (bookedRooms.Count == 0)
        {
            ViewBag.Message = "Not enough rooms available.";
            return View("Index", rooms);
        }

        // Book the rooms
        foreach (var room in bookedRooms)
        {
            room.IsBooked = true;
        }

        ViewBag.Message = $"Successfully booked {bookedRooms.Count} rooms.";
        return View("Index", rooms);
    }

    private List<Room> FindAdjacentRooms(List<Room> roomsOnFloor, int roomsRequired)
    {
        // Check for adjacent rooms
        for (int i = 0; i <= roomsOnFloor.Count - roomsRequired; i++)
        {
            bool areAdjacent = true;
            for (int j = 1; j < roomsRequired; j++)
            {
                if (roomsOnFloor[i + j].RoomNumber != roomsOnFloor[i + j - 1].RoomNumber + 1)
                {
                    areAdjacent = false;
                    break;
                }
            }

            if (areAdjacent)
            {
                return roomsOnFloor.Skip(i).Take(roomsRequired).ToList();
            }
        }

        return null;
    }

    private List<Room> FindBestCombinationAcrossFloors(List<Room> availableRooms, int roomsRequired)
    {
        List<Room> bestCombination = null;
        double minTravelTime = double.MaxValue;

        // Group rooms by floor
        var groupedByFloor = availableRooms.GroupBy(r => r.Floor).OrderBy(g => g.Key).ToList();

        // Check all floors for the best combination
        foreach (var group in groupedByFloor)
        {
            if (group.Count() >= roomsRequired)
            {
                var roomsOnFloor = group.OrderBy(r => r.RoomNumber).ToList();

                // Try all possible combinations of rooms on this floor
                for (int i = 0; i <= roomsOnFloor.Count - roomsRequired; i++)
                {
                    var combination = roomsOnFloor.Skip(i).Take(roomsRequired).ToList();
                    double travelTime = CalculateTravelTime(combination);

                    if (travelTime < minTravelTime)
                    {
                        minTravelTime = travelTime;
                        bestCombination = combination;
                    }
                }
            }
        }

        return bestCombination;
    }

    private List<Room> GetRoomsAcrossFloors(List<Room> availableRooms, int roomsRequired)
    {
        List<Room> bookedRooms = new List<Room>();
        double minTotalTravelTime = double.MaxValue;

        // Sort rooms by floor and room number for easier traversal
        var availableRoomsSorted = availableRooms.OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber).ToList();

        // Try all possible combinations of rooms to find the one with the minimum travel time
        for (int i = 0; i <= availableRoomsSorted.Count - roomsRequired; i++)
        {
            var selectedRooms = availableRoomsSorted.Skip(i).Take(roomsRequired).ToList();
            double totalTravelTime = CalculateTravelTime(selectedRooms);

            if (totalTravelTime < minTotalTravelTime)
            {
                minTotalTravelTime = totalTravelTime;
                bookedRooms = selectedRooms;
            }
        }

        return bookedRooms;
    }

    private double CalculateTravelTime(List<Room> roomsToBook)
    {
        double travelTime = 0;

        // Calculate horizontal and vertical travel time
        for (int i = 0; i < roomsToBook.Count - 1; i++)
        {
            var currentRoom = roomsToBook[i];
            var nextRoom = roomsToBook[i + 1];

            // Horizontal travel time: rooms on the same floor
            if (currentRoom.Floor == nextRoom.Floor)
            {
                travelTime += Math.Abs(currentRoom.RoomNumber - nextRoom.RoomNumber) == 1 ? 1 : 2; // Adjacent rooms (1 minute), else (2 minutes)
            }
            else
            {
                // Vertical travel time: rooms on different floors
                travelTime += 2; // 2 minutes for each floor difference
            }
        }

        return travelTime;
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