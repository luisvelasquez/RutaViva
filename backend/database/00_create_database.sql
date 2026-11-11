-- Ejecutar conectado a master (SSMS / Azure Data Studio / sqlcmd).
-- Ajusta el nombre si usas otra base local.
IF DB_ID(N'RutaViva') IS NULL
BEGIN
    CREATE DATABASE [RutaViva];
END;
GO
