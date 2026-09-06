namespace SkeletonDefender.Editor
{
    public static class RebalanceChecks
    {
        public static void Compare()
        {
            BalanceV062Tests.Run();
            V061Playthrough.RunBalanceComparison();
        }

        public static void Validate()
        {
            BalanceV062Tests.Run();
            EquipmentAndAbilitiesTests.Run();
            V061Playthrough.RunBalanceValidation();
            V061Playthrough.RunBalancedFullMap();
            BuildAutomation.ExportBalance();
        }
    }
}
