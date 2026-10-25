namespace OpenNFS1.Audio
{
    internal static class NarrationRules
    {
        public static string Track(string code) => code switch
        {
            "ALP" => "ALPINE.EAS", "COAST" => "COASTAL.EAS", "CITY" => "CITY.EAS",
            "RUST" => "LOOP1.EAS", "AUT" => "LOOP2.EAS", "VERT" => "LOOP3.EAS",
            "BRUN" => "GOLDRASH.EAS", "TROP" => "INDUSTRI.EAS", "LOST" => "LOOPX.EAS",
            _ => null
        };
        public static string Place(int place) => place is >= 1 and <= 8 ?
            new[] { "FIRST.EAS", "SECOND.EAS", "THIRD.EAS", "FOURTH.EAS", "FIFTH.EAS", "SIXTH.EAS", "SEVENTH.EAS", "EIGHTH.EAS" }[place - 1] : null;
    }
}
