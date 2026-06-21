using System.Data;

public static class DataCache
{
    private static DataTable _cacheDanhSach;
    private static readonly object _lock = new object();
    public static bool IsLoaded
    {
        get
        {
            lock (_lock)
                return _cacheDanhSach != null;
        }
    }
    public static DataTable GetDanhSach()
    {
        lock (_lock)
        {
            return _cacheDanhSach?.Copy(); // tránh sửa trực tiếp
        }
    }
    public static void SetDanhSach(DataTable dt)
    {
        lock (_lock)
        {
            _cacheDanhSach = dt;
        }
    }
    public static void Clear()
    {
        lock (_lock)
        {
            _cacheDanhSach = null;
        }
    }
}