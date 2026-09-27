namespace LithographERP.Domain.Modules.Clients;

public static class ClientBusinessIds
{
    public const string Prefix = "CL";
    public const int SequenceDigits = 6;

    public static string Format(long sequence)
    {
        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence));
        }

        return $"{Prefix}-{sequence.ToString().PadLeft(SequenceDigits, '0')}";
    }
}
