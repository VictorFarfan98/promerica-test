USE master;
GO

IF DB_ID(N'CompanyHierarchyDb') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE CompanyHierarchyDb;');
END;
GO

USE CompanyHierarchyDb;
GO


/* ============================================================
   TABLA
   ============================================================ */

IF OBJECT_ID(N'dbo.PlazaEmpleado', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlazaEmpleado
    (
        CodigoPuesto INT IDENTITY(1, 1) NOT NULL,
        Puesto NVARCHAR(100) NOT NULL,
        NombreEmpleado NVARCHAR(150) NOT NULL,
        CodigoJefe INT NULL,

        CONSTRAINT PK_PlazaEmpleado
            PRIMARY KEY (CodigoPuesto),

        CONSTRAINT FK_PlazaEmpleado_Jefe
            FOREIGN KEY (CodigoJefe)
            REFERENCES dbo.PlazaEmpleado(CodigoPuesto),

        CONSTRAINT CK_PlazaEmpleado_NoPropioJefe
            CHECK
            (
                CodigoJefe IS NULL
                OR CodigoJefe <> CodigoPuesto
            )
    );
END;
GO


/* ============================================================
   ÍNDICE PARA BÚSQUEDA DE SUBORDINADOS
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE
        name = N'IX_PlazaEmpleado_CodigoJefe'
        AND object_id = OBJECT_ID(N'dbo.PlazaEmpleado')
)
BEGIN
    CREATE INDEX IX_PlazaEmpleado_CodigoJefe
        ON dbo.PlazaEmpleado(CodigoJefe);
END;
GO


/* ============================================================
   DATOS DE EJEMPLO
   ============================================================ */

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.PlazaEmpleado
)
BEGIN
    SET IDENTITY_INSERT dbo.PlazaEmpleado ON;

    INSERT INTO dbo.PlazaEmpleado
    (
        CodigoPuesto,
        Puesto,
        NombreEmpleado,
        CodigoJefe
    )
    VALUES
        (1, N'Gerente', N'Pedro', NULL);

    INSERT INTO dbo.PlazaEmpleado
    (
        CodigoPuesto,
        Puesto,
        NombreEmpleado,
        CodigoJefe
    )
    VALUES
        (2, N'Sub Gerente', N'Pablo', 1),
        (4, N'Sub Gerente', N'José', 1);

    INSERT INTO dbo.PlazaEmpleado
    (
        CodigoPuesto,
        Puesto,
        NombreEmpleado,
        CodigoJefe
    )
    VALUES
        (3, N'Supervisor', N'Juan', 2),
        (5, N'Supervisor', N'Carlos', 4),
        (6, N'Supervisor', N'Diego', 4);

    SET IDENTITY_INSERT dbo.PlazaEmpleado OFF;
END;
GO


/* ============================================================
   OBTENER ÁRBOL COMPLETO O SUBÁRBOL

   @CodigoRaiz = NULL:
       Devuelve el árbol completo.

   @CodigoRaiz = valor:
       Devuelve la plaza seleccionada y sus subordinados.
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.usp_PlazaEmpleado_ObtenerArbol
    @CodigoRaiz INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @CodigoRaiz IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM dbo.PlazaEmpleado
           WHERE CodigoPuesto = @CodigoRaiz
       )
    BEGIN
        THROW 50001, N'La plaza raíz indicada no existe.', 1;
    END;

    ;WITH Jerarquia AS
    (
        -- Registros raíz
        SELECT
            plaza.CodigoPuesto,
            plaza.Puesto,
            plaza.NombreEmpleado,
            plaza.CodigoJefe,
            0 AS Nivel,

            CAST
            (
                RIGHT
                (
                    REPLICATE('0', 10)
                    + CONVERT(VARCHAR(10), plaza.CodigoPuesto),
                    10
                )
                AS VARCHAR(8000)
            ) AS RutaOrden
        FROM dbo.PlazaEmpleado AS plaza
        WHERE
            (
                @CodigoRaiz IS NULL
                AND plaza.CodigoJefe IS NULL
            )
            OR
            (
                @CodigoRaiz IS NOT NULL
                AND plaza.CodigoPuesto = @CodigoRaiz
            )

        UNION ALL

        -- Subordinados
        SELECT
            subordinado.CodigoPuesto,
            subordinado.Puesto,
            subordinado.NombreEmpleado,
            subordinado.CodigoJefe,
            jefe.Nivel + 1,

            CAST
            (
                jefe.RutaOrden
                + '/'
                + RIGHT
                (
                    REPLICATE('0', 10)
                    + CONVERT(VARCHAR(10), subordinado.CodigoPuesto),
                    10
                )
                AS VARCHAR(8000)
            ) AS RutaOrden
        FROM dbo.PlazaEmpleado AS subordinado
        INNER JOIN Jerarquia AS jefe
            ON subordinado.CodigoJefe = jefe.CodigoPuesto
    )
    SELECT
        CodigoPuesto,
        Puesto,
        NombreEmpleado,
        CodigoJefe,
        Nivel
    FROM Jerarquia
    ORDER BY RutaOrden
    OPTION (MAXRECURSION 32767);
END;
GO


/* ============================================================
   OBTENER TODOS
   Útil para listados y dropdowns de jefes.
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.usp_PlazaEmpleado_ObtenerTodos
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        plaza.CodigoPuesto,
        plaza.Puesto,
        plaza.NombreEmpleado,
        plaza.CodigoJefe,
        jefe.Puesto AS PuestoJefe,
        jefe.NombreEmpleado AS NombreJefe
    FROM dbo.PlazaEmpleado AS plaza
    LEFT JOIN dbo.PlazaEmpleado AS jefe
        ON jefe.CodigoPuesto = plaza.CodigoJefe
    ORDER BY
        plaza.CodigoPuesto;
END;
GO


/* ============================================================
   OBTENER POR CÓDIGO
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.usp_PlazaEmpleado_ObtenerPorCodigo
    @CodigoPuesto INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        plaza.CodigoPuesto,
        plaza.Puesto,
        plaza.NombreEmpleado,
        plaza.CodigoJefe,
        jefe.Puesto AS PuestoJefe,
        jefe.NombreEmpleado AS NombreJefe
    FROM dbo.PlazaEmpleado AS plaza
    LEFT JOIN dbo.PlazaEmpleado AS jefe
        ON jefe.CodigoPuesto = plaza.CodigoJefe
    WHERE plaza.CodigoPuesto = @CodigoPuesto;
END;
GO


/* ============================================================
   INSERTAR
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.usp_PlazaEmpleado_Insertar
    @Puesto NVARCHAR(100),
    @NombreEmpleado NVARCHAR(150),
    @CodigoJefe INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Puesto = LTRIM(RTRIM(@Puesto));
    SET @NombreEmpleado = LTRIM(RTRIM(@NombreEmpleado));

    IF NULLIF(@Puesto, N'') IS NULL
    BEGIN
        THROW 50002, N'El nombre del puesto es obligatorio.', 1;
    END;

    IF NULLIF(@NombreEmpleado, N'') IS NULL
    BEGIN
        THROW 50003, N'El nombre del empleado es obligatorio.', 1;
    END;

    IF @CodigoJefe IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM dbo.PlazaEmpleado
           WHERE CodigoPuesto = @CodigoJefe
       )
    BEGIN
        THROW 50004, N'El jefe indicado no existe.', 1;
    END;

    INSERT INTO dbo.PlazaEmpleado
    (
        Puesto,
        NombreEmpleado,
        CodigoJefe
    )
    VALUES
    (
        @Puesto,
        @NombreEmpleado,
        @CodigoJefe
    );

    DECLARE @NuevoCodigoPuesto INT =
        CONVERT(INT, SCOPE_IDENTITY());

    SELECT
        CodigoPuesto,
        Puesto,
        NombreEmpleado,
        CodigoJefe
    FROM dbo.PlazaEmpleado
    WHERE CodigoPuesto = @NuevoCodigoPuesto;
END;
GO


/* ============================================================
   MODIFICAR

   Evita:
   - Jefe inexistente
   - Que una plaza sea su propio jefe
   - Ciclos indirectos dentro del árbol
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.usp_PlazaEmpleado_Modificar
    @CodigoPuesto INT,
    @Puesto NVARCHAR(100),
    @NombreEmpleado NVARCHAR(150),
    @CodigoJefe INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @Puesto = LTRIM(RTRIM(@Puesto));
    SET @NombreEmpleado = LTRIM(RTRIM(@NombreEmpleado));

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.PlazaEmpleado
        WHERE CodigoPuesto = @CodigoPuesto
    )
    BEGIN
        THROW 50005, N'La plaza que desea modificar no existe.', 1;
    END;

    IF NULLIF(@Puesto, N'') IS NULL
    BEGIN
        THROW 50006, N'El nombre del puesto es obligatorio.', 1;
    END;

    IF NULLIF(@NombreEmpleado, N'') IS NULL
    BEGIN
        THROW 50007, N'El nombre del empleado es obligatorio.', 1;
    END;

    IF @CodigoJefe = @CodigoPuesto
    BEGIN
        THROW 50008, N'Una plaza no puede ser su propio jefe.', 1;
    END;

    IF @CodigoJefe IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM dbo.PlazaEmpleado
           WHERE CodigoPuesto = @CodigoJefe
       )
    BEGIN
        THROW 50009, N'El jefe indicado no existe.', 1;
    END;

    /*
        Recorre hacia arriba desde el nuevo jefe.

        Si durante el recorrido aparece la plaza que se está
        modificando, la asignación crearía un ciclo.
    */
    IF @CodigoJefe IS NOT NULL
    BEGIN
        DECLARE @CreaCiclo BIT = 0;

        ;WITH CadenaJefes AS
        (
            SELECT
                plaza.CodigoPuesto,
                plaza.CodigoJefe
            FROM dbo.PlazaEmpleado AS plaza
            WHERE plaza.CodigoPuesto = @CodigoJefe

            UNION ALL

            SELECT
                jefe.CodigoPuesto,
                jefe.CodigoJefe
            FROM dbo.PlazaEmpleado AS jefe
            INNER JOIN CadenaJefes AS cadena
                ON jefe.CodigoPuesto = cadena.CodigoJefe
        )
        SELECT
            @CreaCiclo = 1
        FROM CadenaJefes
        WHERE CodigoPuesto = @CodigoPuesto
        OPTION (MAXRECURSION 32767);

        IF @CreaCiclo = 1
        BEGIN
            THROW 50010,
                N'La asignación indicada crearía un ciclo en la jerarquía.',
                1;
        END;
    END;

    UPDATE dbo.PlazaEmpleado
    SET
        Puesto = @Puesto,
        NombreEmpleado = @NombreEmpleado,
        CodigoJefe = @CodigoJefe
    WHERE CodigoPuesto = @CodigoPuesto;

    SELECT
        CodigoPuesto,
        Puesto,
        NombreEmpleado,
        CodigoJefe
    FROM dbo.PlazaEmpleado
    WHERE CodigoPuesto = @CodigoPuesto;
END;
GO


/* ============================================================
   ELIMINAR

   No permite eliminar una plaza que tenga subordinados.
   Primero deben eliminarse o reasignarse.
   ============================================================ */

CREATE OR ALTER PROCEDURE dbo.usp_PlazaEmpleado_Eliminar
    @CodigoPuesto INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.PlazaEmpleado
        WHERE CodigoPuesto = @CodigoPuesto
    )
    BEGIN
        THROW 50011, N'La plaza que desea eliminar no existe.', 1;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.PlazaEmpleado
        WHERE CodigoJefe = @CodigoPuesto
    )
    BEGIN
        THROW 50012,
            N'No se puede eliminar la plaza porque tiene subordinados.',
            1;
    END;

    DELETE FROM dbo.PlazaEmpleado
    OUTPUT
        DELETED.CodigoPuesto,
        DELETED.Puesto,
        DELETED.NombreEmpleado,
        DELETED.CodigoJefe
    WHERE CodigoPuesto = @CodigoPuesto;
END;
GO


/* ============================================================
   PRUEBA FINAL
   ============================================================ */

EXEC dbo.usp_PlazaEmpleado_ObtenerArbol;
GO