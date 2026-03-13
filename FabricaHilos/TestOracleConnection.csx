using Oracle.ManagedDataAccess.Client;

// Script de prueba de conexión Oracle
var connectionString = "Data Source=10.0.7.11:1521/ORCL;User Id=VICMATE;Password=ANGELO1006;";

Console.WriteLine("Iniciando prueba de conexión Oracle...");
Console.WriteLine($"Connection String: {connectionString.Replace("Password=ANGELO1006", "Password=***")}");

try
{
    using var connection = new OracleConnection(connectionString);
    Console.WriteLine("Abriendo conexión...");
    await connection.OpenAsync();
    Console.WriteLine("✓ Conexión exitosa!");

    var query = @"
        SELECT * FROM (
            SELECT 
                G.NUMERO AS NUMERO_RECETA,
                F.ABREVIADO||' '||P.ABREVIADO||' '||V.ABREVIADO||' ('||I.COLOR_DET||')' AS DESCRIPCION_MATERIAL,
                G.LOTE AS CODIGO_LOTE
            FROM H_RECETA_G G,
                 H_FIBRA F,
                 H_PROCESOS P,
                 ITEMPED I,
                 V_TFIBRA T,
                 V_VALPF V,
                 CLIENTES C
            WHERE G.TIPO = 'R'
              AND NVL(G.ESTADO,'1') <> '9'
              AND G.FECHA BETWEEN ADD_MONTHS(TRUNC(SYSDATE),-4) AND TRUNC(SYSDATE)
              AND F.FIBRA = G.FIBRA
              AND P.PROCESO = G.PROCESO
              AND I.NUM_PED = G.NUM_PED
              AND I.NRO = G.ITEM_PED
              AND T.FIBRA = I.TFIBRA
              AND V.TIPO = T.INDPF
              AND V.CODIGO = I.VALPF
              AND C.COD_CLIENTE = G.COD_CLIENTE
              AND TO_CHAR(G.NUMERO) LIKE :codigo || '%'
            ORDER BY G.NUMERO DESC
        )
        WHERE ROWNUM = 1";

    using var command = new OracleCommand(query, connection);
    command.Parameters.Add(new OracleParameter(":codigo", OracleDbType.Varchar2, "3474", ParameterDirection.Input));

    Console.WriteLine("\nEjecutando consulta con código: 3474");
    using var reader = await command.ExecuteReaderAsync();

    if (await reader.ReadAsync())
    {
        Console.WriteLine("✓ Registro encontrado!");
        Console.WriteLine($"  NUMERO_RECETA: {reader["NUMERO_RECETA"]}");
        Console.WriteLine($"  DESCRIPCION_MATERIAL: {reader["DESCRIPCION_MATERIAL"]}");
        Console.WriteLine($"  CODIGO_LOTE: {reader["CODIGO_LOTE"]}");
    }
    else
    {
        Console.WriteLine("✗ No se encontró ningún registro");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Error: {ex.Message}");
    Console.WriteLine($"StackTrace: {ex.StackTrace}");
}

Console.WriteLine("\nPrueba finalizada. Presione Enter para salir...");
Console.ReadLine();
