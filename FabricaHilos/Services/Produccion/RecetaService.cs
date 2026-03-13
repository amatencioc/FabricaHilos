using Oracle.ManagedDataAccess.Client;
using System.Data;
using FabricaHilos.Models.Produccion;

namespace FabricaHilos.Services.Produccion
{
    public class RecetaDto
    {
        public string Numero { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string Lote { get; set; } = string.Empty;
    }

    public class LoteDto
    {
        public string Lote { get; set; } = string.Empty;
        public string Receta { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
    }

    public class MaquinaDto
    {
        public string TipoMaquina { get; set; } = string.Empty;
        public string DescripcionTipoMaquina { get; set; } = string.Empty;
        public string TextoCompleto => $"{TipoMaquina} - {DescripcionTipoMaquina}";
    }

    public class MaquinaIndividualDto
    {
        public string CodigoMaquina { get; set; } = string.Empty;
        public string DescripcionMaquina { get; set; } = string.Empty;
        public string TextoCompleto => $"{CodigoMaquina} - {DescripcionMaquina}";
    }

    public class TituloDto
    {
        public string Titulo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string TextoCompleto => $"{Titulo} - {Descripcion}";
    }

    public class EmpleadoOracleDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string NombreCorto { get; set; } = string.Empty;
        public string TextoCompleto => $"{Codigo} - {NombreCorto}";
    }

    public class PreparatoriaListDto
    {
        public int? LocalId { get; set; }
        public string Receta { get; set; } = string.Empty;
        public string Lote { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string TipoMaquina { get; set; } = string.Empty;
        public string CodigoMaquina { get; set; } = string.Empty;
        public string DescripcionMaquina { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string DescripcionTitulo { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string CodigoOperario { get; set; } = string.Empty;
        public string NombreOperario { get; set; } = string.Empty;
        public string Turno { get; set; } = string.Empty;
        public string PasoManual { get; set; } = string.Empty;
    }

    public class GuardarCerrarResultado
    {
        public bool UpdateExitoso { get; set; }
        public string Codigo { get; set; } = "0";
        public string Mensaje { get; set; } = string.Empty;
    }

    public interface IRecetaService
    {
        Task<RecetaDto?> BuscarRecetaPorCodigoAsync(string codigo);
        Task<LoteDto?> BuscarLotePorCodigoAsync(string codigo);
        Task<List<MaquinaDto>> ObtenerTiposMaquinasAsync();
        Task<List<MaquinaIndividualDto>> ObtenerMaquinasPorTipoAsync(string tipoMaquina);
        Task<List<TituloDto>> ObtenerTitulosAsync();
        Task<List<EmpleadoOracleDto>> ObtenerEmpleadosAsync();
        Task<bool> InsertarPreparatoriaAsync(OrdenProduccion orden);
        Task<decimal> ObtenerPesoTituloAsync(string titulo);
        Task<List<PreparatoriaListDto>> ObtenerPreparatoriasAsync(string? filtroLote = null, string? filtroMaquina = null);
        Task<bool> CerrarPreparatoriaOracleAsync(string? receta, string? lote, string? tpMaq, string? codMaq, string? titulo, DateTime fechaIni);
        Task<bool> AnularPreparatoriaOracleAsync(string? receta, string? lote, string? tpMaq, string? codMaq, string? titulo, DateTime fechaIni);
        Task<bool> ActualizarPreparatoriaOracleAsync(
            string? oldReceta, string? oldLote, string? oldTpMaq, string? oldCodMaq, string? oldTitulo, DateTime fechaIni,
            string? newReceta, string? newLote, string? newTpMaq, string? newCodMaq, string? newTitulo,
            string? cCodigo, string? turno, string? pasoManuar);
        Task<GuardarCerrarResultado> GuardarYCerrarDetalleProduccionAsync(
            string? receta, string? lote, string? tpMaq, string? codMaq, string? titulo, DateTime fechaIni,
            decimal? velocidad, decimal? metraje, int? rolloTacho, decimal? kgNeto);
    }

    public class RecetaService : IRecetaService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RecetaService> _logger;

        public RecetaService(IConfiguration configuration, ILogger<RecetaService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<RecetaDto?> BuscarRecetaPorCodigoAsync(string codigo)
        {
            var connectionString = _configuration.GetConnectionString("OracleConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Oracle connection string not configured");
                return null;
            }

            _logger.LogInformation("Buscando receta con código: {Codigo}", codigo);

            const string query = @"
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

            try
            {
                _logger.LogDebug("Conectando a Oracle...");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();
                _logger.LogDebug("Conexión establecida");

                using var command = new OracleCommand(query, connection);
                command.Parameters.Add(new OracleParameter(":codigo", OracleDbType.Varchar2, codigo, ParameterDirection.Input));

                _logger.LogDebug("Ejecutando consulta...");
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    _logger.LogInformation("Receta encontrada");

                    var resultado = new RecetaDto
                    {
                        Numero = reader["NUMERO_RECETA"]?.ToString() ?? string.Empty,
                        Material = reader["DESCRIPCION_MATERIAL"]?.ToString() ?? string.Empty,
                        Lote = reader["CODIGO_LOTE"]?.ToString() ?? string.Empty
                    };

                    _logger.LogInformation("Receta: {Numero}, Material: {Material}, Lote: {Lote}", 
                        resultado.Numero, resultado.Material, resultado.Lote);

                    return resultado;
                }

                _logger.LogWarning("No se encontró ninguna receta con el código: {Codigo}", codigo);
                return null;
            }
            catch (OracleException oEx)
            {
                _logger.LogError(oEx, "Error de Oracle al buscar receta: {Codigo}. OracleError: {OracleError}", 
                    codigo, oEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al buscar receta en Oracle: {Codigo}", codigo);
                throw;
            }
        }

        public async Task<LoteDto?> BuscarLotePorCodigoAsync(string codigo)
        {
            var connectionString = _configuration.GetConnectionString("OracleConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Oracle connection string not configured");
                return null;
            }

            _logger.LogInformation("Buscando lote con código: {Codigo}", codigo);

            const string query = @"
                SELECT * FROM (
                    SELECT 
                        R.LOTE AS CODIGO_LOTE,
                        NULL AS NUMERO_RECETA,
                        F.ABREVIADO||' '||P.ABREVIADO||' '||V.ABREVIADO AS DESCRIPCION_MATERIAL
                    FROM H_RUTA_LOTE_G R,
                         H_FIBRA F,
                         H_PROCESOS P,
                         V_VALPF V
                    WHERE R.ESTADO = '0'
                      AND F.FIBRA = R.FIBRA
                      AND P.PROCESO = R.PROCESO
                      AND V.TIPO = F.INDPF
                      AND V.CODIGO = R.VALPF
                      AND R.LOTE LIKE :codigo || '%'
                    ORDER BY R.LOTE DESC
                )
                WHERE ROWNUM = 1";

            try
            {
                _logger.LogDebug("Conectando a Oracle para buscar lote...");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();
                _logger.LogDebug("Conexión establecida");

                using var command = new OracleCommand(query, connection);
                command.Parameters.Add(new OracleParameter(":codigo", OracleDbType.Varchar2, codigo, ParameterDirection.Input));

                _logger.LogDebug("Ejecutando consulta de lote...");
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    _logger.LogInformation("Lote encontrado");

                    var resultado = new LoteDto
                    {
                        Lote = reader["CODIGO_LOTE"]?.ToString() ?? string.Empty,
                        Receta = reader["NUMERO_RECETA"]?.ToString() ?? string.Empty,
                        Material = reader["DESCRIPCION_MATERIAL"]?.ToString() ?? string.Empty
                    };

                    _logger.LogInformation("Lote: {Lote}, Receta: {Receta}, Material: {Material}", 
                        resultado.Lote, resultado.Receta, resultado.Material);

                    return resultado;
                }

                _logger.LogWarning("No se encontró ningún lote con el código: {Codigo}", codigo);
                return null;
            }
            catch (OracleException oEx)
            {
                _logger.LogError(oEx, "Error de Oracle al buscar lote: {Codigo}. OracleError: {OracleError}", 
                    codigo, oEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al buscar lote en Oracle: {Codigo}", codigo);
                throw;
            }
        }

        public async Task<List<MaquinaDto>> ObtenerTiposMaquinasAsync()
        {
            var connectionString = _configuration.GetConnectionString("OracleConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Oracle connection string not configured");
                return new List<MaquinaDto>();
            }

            _logger.LogInformation("Obteniendo tipos de máquinas desde V_MAQUINA");

            const string query = @"
                SELECT 
                    TP_MAQ,
                    MAX(DESC_TPMAQ) AS DESC_TPMAQ
                FROM V_MAQUINA
                WHERE AREA = '01'
                GROUP BY TP_MAQ
                ORDER BY TP_MAQ";

            try
            {
                _logger.LogDebug("Conectando a Oracle para obtener tipos de máquinas...");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();
                _logger.LogDebug("Conexión establecida");

                using var command = new OracleCommand(query, connection);
                _logger.LogDebug("Ejecutando consulta de tipos de máquinas...");
                using var reader = await command.ExecuteReaderAsync();

                var maquinas = new List<MaquinaDto>();
                while (await reader.ReadAsync())
                {
                    maquinas.Add(new MaquinaDto
                    {
                        TipoMaquina = reader["TP_MAQ"]?.ToString() ?? string.Empty,
                        DescripcionTipoMaquina = reader["DESC_TPMAQ"]?.ToString() ?? string.Empty
                    });
                }

                _logger.LogInformation("Se obtuvieron {Count} tipos de máquinas", maquinas.Count);
                return maquinas;
            }
            catch (OracleException oEx)
            {
                _logger.LogError(oEx, "Error de Oracle al obtener tipos de máquinas. OracleError: {OracleError}", 
                    oEx.Message);
                return new List<MaquinaDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al obtener tipos de máquinas en Oracle");
                return new List<MaquinaDto>();
            }
        }

        public async Task<List<MaquinaIndividualDto>> ObtenerMaquinasPorTipoAsync(string tipoMaquina)
        {
            var connectionString = _configuration.GetConnectionString("OracleConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Oracle connection string not configured");
                return new List<MaquinaIndividualDto>();
            }

            if (string.IsNullOrEmpty(tipoMaquina))
            {
                _logger.LogWarning("Tipo de máquina no especificado");
                return new List<MaquinaIndividualDto>();
            }

            _logger.LogInformation("Obteniendo máquinas para el tipo: {TipoMaquina}", tipoMaquina);

            const string query = @"
                SELECT 
                    COD_MAQ,
                    DESC_MAQ
                FROM V_MAQUINA
                WHERE TP_MAQ = :tipoMaquina
                  AND AREA = '01'
                ORDER BY COD_MAQ";

            try
            {
                _logger.LogDebug("Conectando a Oracle para obtener máquinas individuales...");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();
                _logger.LogDebug("Conexión establecida");

                using var command = new OracleCommand(query, connection);
                command.Parameters.Add(new OracleParameter(":tipoMaquina", OracleDbType.Varchar2, tipoMaquina, ParameterDirection.Input));

                _logger.LogDebug("Ejecutando consulta de máquinas individuales...");
                using var reader = await command.ExecuteReaderAsync();

                var maquinas = new List<MaquinaIndividualDto>();
                while (await reader.ReadAsync())
                {
                    maquinas.Add(new MaquinaIndividualDto
                    {
                        CodigoMaquina = reader["COD_MAQ"]?.ToString() ?? string.Empty,
                        DescripcionMaquina = reader["DESC_MAQ"]?.ToString() ?? string.Empty
                    });
                }

                _logger.LogInformation("Se obtuvieron {Count} máquinas para el tipo {TipoMaquina}", maquinas.Count, tipoMaquina);
                return maquinas;
            }
            catch (OracleException oEx)
            {
                _logger.LogError(oEx, "Error de Oracle al obtener máquinas. OracleError: {OracleError}", 
                    oEx.Message);
                return new List<MaquinaIndividualDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al obtener máquinas en Oracle");
                return new List<MaquinaIndividualDto>();
            }
        }

        public async Task<List<TituloDto>> ObtenerTitulosAsync()
        {
            var connectionString = _configuration.GetConnectionString("OracleConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Oracle connection string not configured");
                return new List<TituloDto>();
            }

            _logger.LogInformation("Obteniendo títulos desde H_TITULOS");

            const string query = @"
                SELECT T.TITULO, T.DESCRIPCION AS DESC_TITULO
                FROM H_TITULOS T
                WHERE T.TITULO BETWEEN '400' AND '499'
                UNION
                SELECT T.TITULO, T.DESCRIPCION AS DESC_TITULO
                FROM H_TITULOS T
                WHERE T.TITULO = '200'
                ORDER BY 1";

            try
            {
                _logger.LogDebug("Conectando a Oracle para obtener títulos...");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();
                _logger.LogDebug("Conexión establecida");

                using var command = new OracleCommand(query, connection);
                _logger.LogDebug("Ejecutando consulta de títulos...");
                using var reader = await command.ExecuteReaderAsync();

                var titulos = new List<TituloDto>();
                while (await reader.ReadAsync())
                {
                    titulos.Add(new TituloDto
                    {
                        Titulo = reader["TITULO"]?.ToString() ?? string.Empty,
                        Descripcion = reader["DESC_TITULO"]?.ToString() ?? string.Empty
                    });
                }

                _logger.LogInformation("Se obtuvieron {Count} títulos", titulos.Count);
                return titulos;
            }
            catch (OracleException oEx)
            {
                _logger.LogError(oEx, "Error de Oracle al obtener títulos. OracleError: {OracleError}", 
                    oEx.Message);
                return new List<TituloDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al obtener títulos en Oracle");
                return new List<TituloDto>();
            }
        }

        public async Task<List<EmpleadoOracleDto>> ObtenerEmpleadosAsync()
        {
            var connectionString = _configuration.GetConnectionString("OracleConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Oracle connection string not configured");
                return new List<EmpleadoOracleDto>();
            }

            _logger.LogInformation("Obteniendo empleados desde V_PERSONAL");

            const string query = @"
                SELECT V.C_CODIGO, V.NOMBRE_CORTO
                FROM V_PERSONAL V,
                     V_GRAN_CCOSTO C
                WHERE V.SITUACION = '1'
                  AND C.GRAN_CCOSTO = '01'
                  AND C.C_COSTO = 'P140'
                  AND C.C_CODIGO = V.C_CODIGO
                ORDER BY 2";

            try
            {
                _logger.LogDebug("Conectando a Oracle para obtener empleados...");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();
                _logger.LogDebug("Conexión establecida");

                using var command = new OracleCommand(query, connection);
                _logger.LogDebug("Ejecutando consulta de empleados...");
                using var reader = await command.ExecuteReaderAsync();

                var empleados = new List<EmpleadoOracleDto>();
                while (await reader.ReadAsync())
                {
                    empleados.Add(new EmpleadoOracleDto
                    {
                        Codigo = reader["C_CODIGO"]?.ToString() ?? string.Empty,
                        NombreCorto = reader["NOMBRE_CORTO"]?.ToString() ?? string.Empty
                    });
                }

                _logger.LogInformation("Se obtuvieron {Count} empleados", empleados.Count);
                return empleados;
            }
            catch (OracleException oEx)
            {
                _logger.LogError(oEx, "Error de Oracle al obtener empleados. OracleError: {OracleError}", 
                    oEx.Message);
                return new List<EmpleadoOracleDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al obtener empleados en Oracle");
                return new List<EmpleadoOracleDto>();
            }
        }

        public async Task<decimal> ObtenerPesoTituloAsync(string titulo)
        {
            var connectionString = _configuration.GetConnectionString("OracleConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Oracle connection string not configured");
                return 0m;
            }

            const string query = "SELECT PESO FROM H_TITULOS WHERE TITULO = :titulo";

            try
            {
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();

                using var command = new OracleCommand(query, connection);
                command.Parameters.Add(new OracleParameter(":titulo", OracleDbType.Varchar2, titulo, ParameterDirection.Input));

                var result = await command.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    return Convert.ToDecimal(result);

                return 0m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener PESO para título: {Titulo}", titulo);
                return 0m;
            }
        }

        public async Task<bool> InsertarPreparatoriaAsync(OrdenProduccion orden)
        {
            var connectionString = _configuration.GetConnectionString("OracleConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Oracle connection string not configured");
                return false;
            }

            _logger.LogInformation("Insertando preparatoria en H_RPRODUC: Receta={Receta}, Lote={Lote}", 
                orden.CodigoReceta, orden.Lote);

            const string query = @"
                INSERT INTO H_RPRODUC (
                    RECETA,
                    LOTE,
                    TP_MAQ,
                    COD_MAQ,
                    TITULO,
                    FECHA_INI,
                    ESTADO,
                    C_CODIGO,
                    TURNO,
                    PASO_MANUAR,
                    HUSOS_INAC,
                    FECHA_TURNO
                ) VALUES (
                    :receta,
                    :lote,
                    :tp_maq,
                    :cod_maq,
                    :titulo,
                    :fecha_ini,
                    :estado,
                    :c_codigo,
                    :turno,
                    :paso_manuar,
                    :husos_inac,
                    :fecha_turno
                )";

            try
            {
                _logger.LogDebug("Conectando a Oracle para insertar preparatoria...");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();
                _logger.LogDebug("Conexión establecida");

                using var command = new OracleCommand(query, connection);

                // Agregar parámetros
                command.Parameters.Add(new OracleParameter(":receta", OracleDbType.Varchar2, orden.CodigoReceta ?? string.Empty, ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter(":lote", OracleDbType.Varchar2, orden.Lote ?? string.Empty, ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter(":tp_maq", OracleDbType.Varchar2, orden.CodigoMaquina ?? string.Empty, ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter(":cod_maq", OracleDbType.Varchar2, orden.Maquina ?? string.Empty, ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter(":titulo", OracleDbType.Varchar2, orden.Titulo ?? string.Empty, ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter(":fecha_ini", OracleDbType.Date, orden.FechaInicio, ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter(":estado", OracleDbType.Varchar2, "1", ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter(":c_codigo", OracleDbType.Varchar2, orden.EmpleadoId ?? string.Empty, ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter(":turno", OracleDbType.Varchar2, orden.Turno ?? string.Empty, ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter(":paso_manuar", OracleDbType.Varchar2, orden.PasoManuar ?? string.Empty, ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter(":husos_inac", OracleDbType.Int32, 0, ParameterDirection.Input));
                command.Parameters.Add(new OracleParameter(":fecha_turno", OracleDbType.Varchar2, DateTime.Now.ToString("dd/MM/yyyy"), ParameterDirection.Input));

                _logger.LogDebug("Ejecutando INSERT en H_RPRODUC...");
                var rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation("Preparatoria insertada exitosamente en H_RPRODUC. Filas afectadas: {RowsAffected}", rowsAffected);
                    return true;
                }
                else
                {
                    _logger.LogWarning("No se insertó ningún registro en H_RPRODUC");
                    return false;
                }
            }
            catch (OracleException oEx)
            {
                _logger.LogError(oEx, "Error de Oracle al insertar preparatoria. OracleError: {OracleError}", 
                    oEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al insertar preparatoria en Oracle");
                return false;
            }
        }

        public async Task<List<PreparatoriaListDto>> ObtenerPreparatoriasAsync(string? filtroLote = null, string? filtroMaquina = null)
        {
            var connectionString = _configuration.GetConnectionString("OracleConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Oracle connection string not configured");
                return new List<PreparatoriaListDto>();
            }

            _logger.LogInformation("Obteniendo preparatorias desde H_RPRODUC con estado = '1'");

            var query = @"
                SELECT 
                    R.RECETA,
                    R.LOTE,
                    R.TP_MAQ,
                    R.COD_MAQ,
                    R.TITULO,
                    R.FECHA_INI,
                    R.ESTADO,
                    R.C_CODIGO,
                    R.TURNO,
                    R.PASO_MANUAR,
                    M.DESC_MAQ,
                    T.DESCRIPCION AS DESC_TITULO,
                    P.NOMBRE_CORTO AS NOMBRE_OPERARIO,
                    CASE
                        WHEN R.RECETA IS NOT NULL THEN
                            (SELECT F2.ABREVIADO||' '||P2.ABREVIADO||' '||V2.ABREVIADO||' ('||I2.COLOR_DET||')'
                             FROM H_RECETA_G G2, H_FIBRA F2, H_PROCESOS P2, ITEMPED I2, V_TFIBRA T2, V_VALPF V2, CLIENTES C2
                             WHERE TO_CHAR(G2.NUMERO) = TRIM(TO_CHAR(R.RECETA))
                               AND G2.TIPO = 'R'
                               AND NVL(G2.ESTADO,'1') <> '9'
                               AND F2.FIBRA = G2.FIBRA
                               AND P2.PROCESO = G2.PROCESO
                               AND I2.NUM_PED = G2.NUM_PED
                               AND I2.NRO = G2.ITEM_PED
                               AND T2.FIBRA = I2.TFIBRA
                               AND V2.TIPO = T2.INDPF
                               AND V2.CODIGO = I2.VALPF
                               AND ROWNUM = 1)
                        ELSE
                            (SELECT F3.ABREVIADO||' '||PR3.ABREVIADO||' '||V3.ABREVIADO
                             FROM H_RUTA_LOTE_G RL3, H_FIBRA F3, H_PROCESOS PR3, V_VALPF V3
                             WHERE RL3.LOTE = R.LOTE
                               AND RL3.ESTADO = '0'
                               AND F3.FIBRA = RL3.FIBRA
                               AND PR3.PROCESO = RL3.PROCESO
                               AND V3.TIPO = F3.INDPF
                               AND V3.CODIGO = RL3.VALPF
                               AND ROWNUM = 1)
                    END AS MATERIAL
                FROM H_RPRODUC R
                LEFT JOIN V_MAQUINA M ON M.COD_MAQ = R.COD_MAQ AND M.AREA = '01'
                LEFT JOIN H_TITULOS T ON T.TITULO = R.TITULO
                LEFT JOIN V_PERSONAL P ON P.C_CODIGO = R.C_CODIGO
                WHERE R.ESTADO = '1'";

            // Agregar filtros si existen
            if (!string.IsNullOrEmpty(filtroLote))
            {
                query += " AND R.LOTE LIKE :filtroLote || '%'";
            }
            if (!string.IsNullOrEmpty(filtroMaquina))
            {
                query += " AND R.COD_MAQ LIKE :filtroMaquina || '%'";
            }

            query += " ORDER BY R.FECHA_INI DESC";

            try
            {
                _logger.LogDebug("Conectando a Oracle para obtener preparatorias...");
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();
                _logger.LogDebug("Conexión establecida");

                using var command = new OracleCommand(query, connection);

                // Agregar parámetros de filtro si existen
                if (!string.IsNullOrEmpty(filtroLote))
                {
                    command.Parameters.Add(new OracleParameter(":filtroLote", OracleDbType.Varchar2, filtroLote, ParameterDirection.Input));
                }
                if (!string.IsNullOrEmpty(filtroMaquina))
                {
                    command.Parameters.Add(new OracleParameter(":filtroMaquina", OracleDbType.Varchar2, filtroMaquina, ParameterDirection.Input));
                }

                _logger.LogDebug("Ejecutando consulta de preparatorias...");
                using var reader = await command.ExecuteReaderAsync();

                var preparatorias = new List<PreparatoriaListDto>();
                while (await reader.ReadAsync())
                {
                    var fechaIni = reader["FECHA_INI"] != DBNull.Value 
                        ? Convert.ToDateTime(reader["FECHA_INI"]) 
                        : DateTime.MinValue;

                    preparatorias.Add(new PreparatoriaListDto
                    {
                        Receta = reader["RECETA"]?.ToString()?.Trim() ?? string.Empty,
                        Lote = reader["LOTE"]?.ToString()?.Trim() ?? string.Empty,
                        Material = reader["MATERIAL"]?.ToString()?.Trim() ?? string.Empty,
                        TipoMaquina = reader["TP_MAQ"]?.ToString()?.Trim() ?? string.Empty,
                        CodigoMaquina = reader["COD_MAQ"]?.ToString()?.Trim() ?? string.Empty,
                        DescripcionMaquina = reader["DESC_MAQ"]?.ToString()?.Trim() ?? string.Empty,
                        Titulo = reader["TITULO"]?.ToString()?.Trim() ?? string.Empty,
                        DescripcionTitulo = reader["DESC_TITULO"]?.ToString()?.Trim() ?? string.Empty,
                        FechaInicio = fechaIni,
                        Estado = reader["ESTADO"]?.ToString()?.Trim() ?? string.Empty,
                        CodigoOperario = reader["C_CODIGO"]?.ToString()?.Trim() ?? string.Empty,
                        NombreOperario = reader["NOMBRE_OPERARIO"]?.ToString()?.Trim() ?? string.Empty,
                        Turno = reader["TURNO"]?.ToString()?.Trim() ?? string.Empty,
                        PasoManual = reader["PASO_MANUAR"]?.ToString()?.Trim() ?? string.Empty
                    });
                }

                _logger.LogInformation("Se obtuvieron {Count} preparatorias", preparatorias.Count);
                return preparatorias;
            }
            catch (OracleException oEx)
            {
                _logger.LogError(oEx, "Error de Oracle al obtener preparatorias. OracleError: {OracleError}", 
                    oEx.Message);
                return new List<PreparatoriaListDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al obtener preparatorias en Oracle");
                return new List<PreparatoriaListDto>();
            }
        }

        public async Task<bool> CerrarPreparatoriaOracleAsync(string? receta, string? lote, string? tpMaq, string? codMaq, string? titulo, DateTime fechaIni)
        {
            var connectionString = _configuration.GetConnectionString("OracleConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Oracle connection string not configured");
                return false;
            }

            _logger.LogInformation("Cerrando preparatoria en Oracle: Receta={Receta}, Lote={Lote}, TpMaq={TpMaq}, CodMaq={CodMaq}, Titulo={Titulo}, FechaIni={FechaIni}",
                receta, lote, tpMaq, codMaq, titulo, fechaIni);

            const string query = @"
                UPDATE H_RPRODUC SET ESTADO = '3',
                                    FECHA_FIN = :fechaFin
                WHERE NVL(TO_CHAR(RECETA), ' ')                    = NVL(:receta, ' ')
                  AND LOTE                                          = :lote
                  AND TP_MAQ                                        = :tpMaq
                  AND COD_MAQ                                       = :codMaq
                  AND TITULO                                        = :titulo
                  AND TO_CHAR(FECHA_INI, 'YYYY-MM-DD HH24:MI:SS') = :fechaIni
                  AND ESTADO = '1'";

            try
            {
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();

                using var command = new OracleCommand(query, connection);

                static object Str(string? v) => string.IsNullOrEmpty(v) ? DBNull.Value : (object)v;

                command.Parameters.Add(new OracleParameter(":fechaFin", OracleDbType.Date) { Value = DateTime.Now });
                command.Parameters.Add(new OracleParameter(":receta",   OracleDbType.Varchar2) { Value = Str(receta) });
                command.Parameters.Add(new OracleParameter(":lote",     OracleDbType.Varchar2) { Value = Str(lote) });
                command.Parameters.Add(new OracleParameter(":tpMaq",    OracleDbType.Varchar2) { Value = Str(tpMaq) });
                command.Parameters.Add(new OracleParameter(":codMaq",   OracleDbType.Varchar2) { Value = Str(codMaq) });
                command.Parameters.Add(new OracleParameter(":titulo",   OracleDbType.Varchar2) { Value = Str(titulo) });
                command.Parameters.Add(new OracleParameter(":fechaIni", OracleDbType.Varchar2) { Value = fechaIni.ToString("yyyy-MM-dd HH:mm:ss") });

                var rowsAffected = await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Preparatoria cerrada en Oracle. Filas afectadas: {Rows}", rowsAffected);
                return rowsAffected > 0;
            }
            catch (OracleException oEx)
            {
                _logger.LogError(oEx, "Error de Oracle al cerrar preparatoria: {Receta}", receta);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al cerrar preparatoria: {Receta}", receta);
                return false;
            }
        }

        public async Task<bool> AnularPreparatoriaOracleAsync(string? receta, string? lote, string? tpMaq, string? codMaq, string? titulo, DateTime fechaIni)
        {
            var connectionString = _configuration.GetConnectionString("OracleConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Oracle connection string not configured");
                return false;
            }

            _logger.LogInformation("Anulando preparatoria en Oracle: Receta={Receta}, Lote={Lote}, TpMaq={TpMaq}, CodMaq={CodMaq}, Titulo={Titulo}, FechaIni={FechaIni}",
                receta, lote, tpMaq, codMaq, titulo, fechaIni);

            // TO_CHAR a nivel de segundos: FECHA_INI fue insertada con el mismo DateTime.Now
            // de la app (ya no usa SYSDATE), por lo que los valores coincidirán exactamente.
            const string query = @"
                UPDATE H_RPRODUC SET ESTADO = '9',
                                    FECHA_FIN = :fechaFin
                WHERE NVL(TO_CHAR(RECETA), ' ')                    = NVL(:receta, ' ')
                  AND LOTE                                          = :lote
                  AND TP_MAQ                                        = :tpMaq
                  AND COD_MAQ                                       = :codMaq
                  AND TITULO                                        = :titulo
                  AND TO_CHAR(FECHA_INI, 'YYYY-MM-DD HH24:MI:SS') = :fechaIni
                  AND ESTADO = '1'";

            try
            {
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();

                using var command = new OracleCommand(query, connection);

                static object Str(string? v) => string.IsNullOrEmpty(v) ? DBNull.Value : (object)v;

                command.Parameters.Add(new OracleParameter(":fechaFin", OracleDbType.Date) { Value = DateTime.Now });
                command.Parameters.Add(new OracleParameter(":receta",  OracleDbType.Varchar2) { Value = Str(receta) });
                command.Parameters.Add(new OracleParameter(":lote",    OracleDbType.Varchar2) { Value = Str(lote) });
                command.Parameters.Add(new OracleParameter(":tpMaq",   OracleDbType.Varchar2) { Value = Str(tpMaq) });
                command.Parameters.Add(new OracleParameter(":codMaq",  OracleDbType.Varchar2) { Value = Str(codMaq) });
                command.Parameters.Add(new OracleParameter(":titulo",  OracleDbType.Varchar2) { Value = Str(titulo) });
                command.Parameters.Add(new OracleParameter(":fechaIni", OracleDbType.Varchar2) { Value = fechaIni.ToString("yyyy-MM-dd HH:mm:ss") });

                var rowsAffected = await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Preparatoria anulada en Oracle. Filas afectadas: {Rows}", rowsAffected);
                return rowsAffected > 0;
            }
            catch (OracleException oEx)
            {
                _logger.LogError(oEx, "Error de Oracle al anular preparatoria: {Receta}", receta);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al anular preparatoria: {Receta}", receta);
                return false;
            }
        }

        public async Task<bool> ActualizarPreparatoriaOracleAsync(
            string? oldReceta, string? oldLote, string? oldTpMaq, string? oldCodMaq, string? oldTitulo, DateTime fechaIni,
            string? newReceta, string? newLote, string? newTpMaq, string? newCodMaq, string? newTitulo,
            string? cCodigo, string? turno, string? pasoManuar)
        {
            var connectionString = _configuration.GetConnectionString("OracleConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Oracle connection string not configured");
                return false;
            }

            _logger.LogInformation(
                "Actualizando preparatoria en Oracle. WHERE: Receta={OldReceta}, Lote={OldLote}, TpMaq={OldTpMaq}, CodMaq={OldCodMaq}, Titulo={OldTitulo}, FechaIni={FechaIni}",
                oldReceta, oldLote, oldTpMaq, oldCodMaq, oldTitulo, fechaIni);

            // SET: campos editables. FECHA_INI y ESTADO no se modifican.
            // WHERE: usa TO_CHAR a segundos — FECHA_INI fue insertada con DateTime.Now de la app
            // (no SYSDATE), así ambos valores son idénticos.
            const string query = @"
                UPDATE H_RPRODUC
                SET
                    RECETA      = :newReceta,
                    LOTE        = :newLote,
                    TP_MAQ      = :newTpMaq,
                    COD_MAQ     = :newCodMaq,
                    TITULO      = :newTitulo,
                    C_CODIGO    = :cCodigo,
                    TURNO       = :turno,
                    PASO_MANUAR = :pasoManuar
                WHERE NVL(TO_CHAR(RECETA), ' ')                    = NVL(:oldReceta, ' ')
                  AND LOTE                                          = :oldLote
                  AND TP_MAQ                                        = :oldTpMaq
                  AND COD_MAQ                                       = :oldCodMaq
                  AND TITULO                                        = :oldTitulo
                  AND TO_CHAR(FECHA_INI, 'YYYY-MM-DD HH24:MI:SS') = :fechaIni
                  AND ESTADO  = '1'";

            try
            {
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();

                using var command = new OracleCommand(query, connection);

                static object Str(string? v) => string.IsNullOrEmpty(v) ? DBNull.Value : (object)v;

                // SET
                command.Parameters.Add(new OracleParameter(":newReceta",   OracleDbType.Varchar2) { Value = Str(newReceta) });
                command.Parameters.Add(new OracleParameter(":newLote",     OracleDbType.Varchar2) { Value = Str(newLote) });
                command.Parameters.Add(new OracleParameter(":newTpMaq",    OracleDbType.Varchar2) { Value = Str(newTpMaq) });
                command.Parameters.Add(new OracleParameter(":newCodMaq",   OracleDbType.Varchar2) { Value = Str(newCodMaq) });
                command.Parameters.Add(new OracleParameter(":newTitulo",   OracleDbType.Varchar2) { Value = Str(newTitulo) });
                command.Parameters.Add(new OracleParameter(":cCodigo",     OracleDbType.Varchar2) { Value = Str(cCodigo) });
                command.Parameters.Add(new OracleParameter(":turno",       OracleDbType.Varchar2) { Value = Str(turno) });
                command.Parameters.Add(new OracleParameter(":pasoManuar",  OracleDbType.Varchar2) { Value = Str(pasoManuar) });
                // WHERE
                command.Parameters.Add(new OracleParameter(":oldReceta",   OracleDbType.Varchar2) { Value = Str(oldReceta) });
                command.Parameters.Add(new OracleParameter(":oldLote",     OracleDbType.Varchar2) { Value = Str(oldLote) });
                command.Parameters.Add(new OracleParameter(":oldTpMaq",    OracleDbType.Varchar2) { Value = Str(oldTpMaq) });
                command.Parameters.Add(new OracleParameter(":oldCodMaq",   OracleDbType.Varchar2) { Value = Str(oldCodMaq) });
                command.Parameters.Add(new OracleParameter(":oldTitulo",   OracleDbType.Varchar2) { Value = Str(oldTitulo) });
                command.Parameters.Add(new OracleParameter(":fechaIni",    OracleDbType.Varchar2) { Value = fechaIni.ToString("yyyy-MM-dd HH:mm:ss") });

                var rowsAffected = await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Preparatoria actualizada en Oracle. Filas afectadas: {Rows}", rowsAffected);
                return rowsAffected > 0;
            }
            catch (OracleException oEx)
            {
                _logger.LogError(oEx, "Error de Oracle al actualizar preparatoria. OracleError: {OracleError}", oEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al actualizar preparatoria en Oracle");
                return false;
            }
        }

        public async Task<GuardarCerrarResultado> GuardarYCerrarDetalleProduccionAsync(
            string? receta, string? lote, string? tpMaq, string? codMaq, string? titulo, DateTime fechaIni,
            decimal? velocidad, decimal? metraje, int? rolloTacho, decimal? kgNeto)
        {
            var connectionString = _configuration.GetConnectionString("OracleConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Oracle connection string not configured");
                return new GuardarCerrarResultado { UpdateExitoso = false };
            }

            const string query = @"
                UPDATE H_RPRODUC
                SET VELOCIDAD = :velocidad,
                    METRAJE   = :metraje,
                    UNIDADES  = :unidades,
                    PESO_NETO = :kgPeso,
                    ESTADO    = '3',
                    FECHA_FIN = :fechaFin
                WHERE NVL(TO_CHAR(RECETA), ' ')                    = NVL(:receta, ' ')
                  AND LOTE                                          = :lote
                  AND TP_MAQ                                        = :tpMaq
                  AND COD_MAQ                                       = :codMaq
                  AND TITULO                                        = :titulo
                  AND TO_CHAR(FECHA_INI, 'YYYY-MM-DD HH24:MI:SS') = :fechaIni
                  AND ESTADO = '1'";

            try
            {
                using var connection = new OracleConnection(connectionString);
                await connection.OpenAsync();

                using var command = new OracleCommand(query, connection);

                static object Str(string? v) => string.IsNullOrEmpty(v) ? DBNull.Value : (object)v;
                static object Dec(decimal? v) => v.HasValue ? (object)v.Value : DBNull.Value;
                static object Int(int? v) => v.HasValue ? (object)v.Value : DBNull.Value;

                command.Parameters.Add(new OracleParameter(":velocidad", OracleDbType.Decimal)    { Value = Dec(velocidad) });
                command.Parameters.Add(new OracleParameter(":metraje",   OracleDbType.Decimal)    { Value = Dec(metraje) });
                command.Parameters.Add(new OracleParameter(":unidades",  OracleDbType.Int32)      { Value = Int(rolloTacho) });
                command.Parameters.Add(new OracleParameter(":kgPeso",    OracleDbType.Decimal)    { Value = Dec(kgNeto) });
                command.Parameters.Add(new OracleParameter(":fechaFin",  OracleDbType.Date)       { Value = DateTime.Now });
                command.Parameters.Add(new OracleParameter(":receta",    OracleDbType.Varchar2)   { Value = Str(receta) });
                command.Parameters.Add(new OracleParameter(":lote",      OracleDbType.Varchar2)   { Value = Str(lote) });
                command.Parameters.Add(new OracleParameter(":tpMaq",     OracleDbType.Varchar2)   { Value = Str(tpMaq) });
                command.Parameters.Add(new OracleParameter(":codMaq",    OracleDbType.Varchar2)   { Value = Str(codMaq) });
                command.Parameters.Add(new OracleParameter(":titulo",    OracleDbType.Varchar2)   { Value = Str(titulo) });
                command.Parameters.Add(new OracleParameter(":fechaIni",  OracleDbType.Varchar2)   { Value = fechaIni.ToString("yyyy-MM-dd HH:mm:ss") });

                var rowsAffected = await command.ExecuteNonQueryAsync();
                _logger.LogInformation("GuardarYCerrar: filas afectadas en H_RPRODUC = {Rows}", rowsAffected);

                if (rowsAffected <= 0)
                    return new GuardarCerrarResultado { UpdateExitoso = false };

                // Ejecutar SP_CALCULAR_PROD_ESP_TEO tras el UPDATE exitoso
                using var procCommand = new OracleCommand("SIG.PKG_PROD_RUTINAS.SP_CALCULAR_PROD_ESP_TEO", connection);
                procCommand.CommandType = CommandType.StoredProcedure;
                procCommand.BindByName  = true;

                procCommand.Parameters.Add(new OracleParameter("pi_receta",    OracleDbType.Varchar2, 200) { Direction = ParameterDirection.Input,  Value = Str(receta) });
                procCommand.Parameters.Add(new OracleParameter("pi_lote",      OracleDbType.Varchar2, 200) { Direction = ParameterDirection.Input,  Value = Str(lote) });
                procCommand.Parameters.Add(new OracleParameter("pi_tp_maq",    OracleDbType.Varchar2, 10)  { Direction = ParameterDirection.Input,  Value = Str(tpMaq) });
                procCommand.Parameters.Add(new OracleParameter("pi_cod_maq",   OracleDbType.Varchar2, 20)  { Direction = ParameterDirection.Input,  Value = Str(codMaq) });
                procCommand.Parameters.Add(new OracleParameter("pi_titulo",    OracleDbType.Varchar2, 20)  { Direction = ParameterDirection.Input,  Value = Str(titulo) });
                procCommand.Parameters.Add(new OracleParameter("pi_fecha_ini", OracleDbType.Varchar2, 30)  { Direction = ParameterDirection.Input,  Value = fechaIni.ToString("yyyy-MM-dd HH:mm:ss") });
                var poResultado = new OracleParameter("po_resultado", OracleDbType.Varchar2, 4000) { Direction = ParameterDirection.Output };
                procCommand.Parameters.Add(poResultado);

                await procCommand.ExecuteNonQueryAsync();

                var resultadoStr = poResultado.Value?.ToString() ?? "0|";
                var sepIdx  = resultadoStr.IndexOf('|');
                var codigo  = sepIdx > 0  ? resultadoStr[..sepIdx]       : "0";
                var mensaje = sepIdx >= 0 ? resultadoStr[(sepIdx + 1)..] : string.Empty;

                _logger.LogInformation("SP_CALCULAR_PROD_ESP_TEO resultado: Codigo={Codigo}, Mensaje={Mensaje}", codigo, mensaje);

                return new GuardarCerrarResultado { UpdateExitoso = true, Codigo = codigo, Mensaje = mensaje };
            }
            catch (OracleException oEx)
            {
                _logger.LogError(oEx, "Error de Oracle en GuardarYCerrarDetalleProduccion. OracleError: {OracleError}", oEx.Message);
                return new GuardarCerrarResultado { UpdateExitoso = false };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general en GuardarYCerrarDetalleProduccion");
                return new GuardarCerrarResultado { UpdateExitoso = false };
            }
        }
    }
}
