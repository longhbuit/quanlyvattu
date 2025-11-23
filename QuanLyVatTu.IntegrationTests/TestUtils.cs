namespace QuanLyVatTu.IntegrationTests;

public static class TestUtils
{
    // Delegate to the main project's DotEnvLoader to avoid duplication.
    public static void LoadDotEnv() => DotEnvLoader.LoadDotEnv();
}
