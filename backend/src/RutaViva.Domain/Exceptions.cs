namespace RutaViva.Domain;

public sealed class BusinessException(string message) : Exception(message);
public sealed class ConflictException(string message) : Exception(message);
public sealed class MissingException(string message) : Exception(message);
