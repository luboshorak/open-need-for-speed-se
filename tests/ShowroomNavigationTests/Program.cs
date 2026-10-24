using System;
using OpenNFS1.UI.Screens;
using Page = OpenNFS1.UI.Screens.ShowroomNavigator.Page;

static class Program
{
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Main()
    {
        var nav = new ShowroomNavigator("Supra");
        Assert(nav.CarName == "Supra", "Wrong selected car on showroom entry.");

        nav.GoTo(Page.General);
        nav.ChangePhoto(true, 1);
        nav.ChangePhoto(false, 3);
        Assert(nav.TopPhoto == 1 && nav.BottomPhoto == 3,
            "The two General photo galleries are not independent.");

        nav.ChangeCar(1);
        Assert(nav.CarName == "911", "Next Car did not change the actual car.");
        Assert(nav.CurrentPage == Page.General, "Next Car unexpectedly left the detail page.");
        Assert(nav.TopPhoto == 0 && nav.BottomPhoto == 0,
            "New vehicle reused the previous car's photo index.");

        nav.ChangePhoto(false, -1);
        Assert(nav.BottomPhoto == 4 && nav.TopPhoto == 0, "Photo wrap is incorrect.");
        Assert(nav.CarName == "911", "Changing a photo changed the selected vehicle.");

        nav.GoTo(Page.Mechanical);
        nav.ChangeSection(1);
        Assert(nav.Section == 1 && nav.CarName == "911",
            "Changing a mechanical diagram unexpectedly changed car.");
        nav.ChangeCar(-1);
        Assert(nav.CarName == "Supra" && nav.Section == 0 &&
            nav.CurrentPage == Page.Mechanical, "Previous Car did not preserve detail page.");

        Assert(!nav.Back() && nav.CurrentPage == Page.Index,
            "Back from a detail must return to the car index.");
        Assert(nav.Back(), "Back from car index must exit showroom.");

        nav.ChangeCar(-100);
        Assert(Array.Exists(ShowroomNavigator.Cars, x => x == nav.CarName),
            "Large negative car step left invalid vehicle.");
        nav.GoTo(Page.Performance);
        nav.ChangeSection(-1);
        Assert(nav.Section == 2, "Performance diagram did not wrap backwards.");

        Console.WriteLine("PASS: car arrows, persistent selection, independent photos, page back, and diagram navigation");
    }
}