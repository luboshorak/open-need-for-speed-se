using System;

namespace OpenNFS1.UI.Screens
{
    /// <summary>
    /// One authoritative state machine for original car selection and
    /// showroom detail panels. The bottom arrows ALWAYS change vehicle;
    /// only the small arrows inside artwork change photo/diagram variants.
    /// </summary>
    internal sealed class ShowroomNavigator
    {
        internal enum Page { Index, General, History, Mechanical, Performance }

        public static readonly string[] Cars =
            { "RX7", "NSX", "Supra", "911", "ZR1", "Viper", "F512", "Diablo" };

        public int CarIndex { get; private set; }
        public string CarName => Cars[CarIndex];
        public Page CurrentPage { get; private set; }
        public int TopPhoto { get; private set; }
        public int BottomPhoto { get; private set; }
        public int Section { get; private set; }

        public ShowroomNavigator(string selectedVehicle)
        {
            int index = Array.FindIndex(Cars, name => string.Equals(
                name, selectedVehicle, StringComparison.OrdinalIgnoreCase));
            CarIndex = index >= 0 ? index : 0;
            CurrentPage = Page.Index;
        }

        public void ChangeCar(int delta)
        {
            CarIndex = (CarIndex + (delta % Cars.Length) + Cars.Length) % Cars.Length;
            ResetDetail();
        }

        public void GoTo(Page page)
        {
            CurrentPage = page;
            ResetDetail();
        }

        public bool Back()
        {
            if (CurrentPage == Page.Index) return true;
            GoTo(Page.Index);
            return false;
        }

        public void ChangePhoto(bool upper, int delta)
        {
            if (CurrentPage != Page.General) return;
            if (upper) TopPhoto = (TopPhoto + (delta % 5) + 5) % 5;
            else BottomPhoto = (BottomPhoto + (delta % 5) + 5) % 5;
        }

        public void ChangeSection(int delta)
        {
            if (CurrentPage != Page.Mechanical && CurrentPage != Page.Performance) return;
            Section = (Section + (delta % 3) + 3) % 3;
        }

        private void ResetDetail()
        {
            TopPhoto = 0;
            BottomPhoto = 0;
            Section = 0;
        }
    }
}
