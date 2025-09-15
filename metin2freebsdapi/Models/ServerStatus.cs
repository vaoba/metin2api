namespace metin2freebsdapi.Models;

public record ServerStatus(bool Online, int Total, int Red, int Yellow, int Blue, int Accounts);