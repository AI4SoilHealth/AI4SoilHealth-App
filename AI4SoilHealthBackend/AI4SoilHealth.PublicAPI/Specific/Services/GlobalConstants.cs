public static class GlobalConstants {
#if DEBUG
    // net use \\maps.ai4soilhealth.eu\uploads  /user:user password /persistent:yes
    // net use \\maps.ai4soilhealth.eu\file_uploads  /user:user password /persistent:yes
    public const string imageDir = "\\\\maps.ai4soilhealth.eu\\uploads\\";
    public const string fileDir = "\\\\maps.ai4soilhealth.eu\\file_uploads\\";
    public const string ai4soilhealthDir = "C:\\ai4soilhealth\\";
    public const string tmpDir = "C:\\tmp\\";
#else
    public const string imageDir = "/data/uploads/";
    public const string fileDir  = "/data/file_uploads";
    public const string ai4soilhealthDir = "/data/ai4soilhealth/";
    public const string tmpDir = "/tmp";
#endif
}