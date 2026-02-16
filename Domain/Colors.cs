using Microsoft.Xna.Framework;


namespace RaidWeekPlanner.Domain
{
    public static class Colors
    {
        public static Color None => Color.Black;
        public static Color Todo => Color.FromNonPremultiplied(215, 166, 157, 160);
        public static Color Planned => Color.FromNonPremultiplied(161, 130, 177, 255);
        public static Color Done => Color.FromNonPremultiplied(130, 177, 161, 175);
    }
}
