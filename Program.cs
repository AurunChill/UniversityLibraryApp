namespace LibraryApp;
using LibraryApp.Data;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        DatabaseInitializer.EnsureCreated();  
    }    
}