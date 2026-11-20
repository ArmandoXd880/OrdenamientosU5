namespace OrdenamientosU5;

public sealed class FormularioBanco : Form
{
    private static readonly Color Azul = Color.FromArgb(24, 54, 89);
    private static readonly int[] Tamanos = [10_000, 50_000, 100_000];
    private static readonly Condicion[] Condiciones = Enum.GetValues<Condicion>();

    private readonly ComboBox tamano = CrearSelector();
    private readonly ComboBox condicion = CrearSelector();
    private readonly NumericUpDown repeticiones = new()
    {
        Minimum = 1, Maximum = 9, Value = 3, Width = 49, Margin = new Padding(4, 5, 18, 2)
    };
    private readonly Button ejecutar = CrearBoton("Ejecutar", principal: true);
    private readonly Button cancelar = CrearBoton("Cancelar");
    private readonly Button exportar = CrearBoton("Exportar CSV");
    private readonly Label estado = new()
    {
        Text = "Listo. Selecciona el tamaño y la condición de entrada.",
        AutoSize = true, Margin = new Padding(2, 9, 0, 0), ForeColor = Azul
    };
    private readonly DataGridView tabla = new()
    {
        Dock = DockStyle.Fill,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        RowHeadersVisible = false,
        RowTemplate = { Height = 29 },
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        EnableHeadersVisualStyles = false,
        ColumnHeadersHeight = 36,
        GridColor = Color.FromArgb(224, 230, 236),
        Margin = new Padding(0, 8, 0, 0)
    };

    private IReadOnlyList<Resultado> resultados = [];
    private CancellationTokenSource? cancelacion;

    public FormularioBanco()
    {
        Text = "Banco de pruebas de ordenamiento — Unidad 5";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(930, 560);
        ClientSize = new Size(1040, 650);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(245, 248, 251);

        tamano.Items.AddRange(["10 000", "50 000", "100 000", "Todos"]);
        condicion.Items.AddRange(["Aleatorio", "Ordenado", "Inverso", "Todas"]);
        tamano.SelectedIndex = 0;
        condicion.SelectedIndex = 3;
        cancelar.Enabled = false;
        exportar.Enabled = false;
        ejecutar.Click += Ejecutar;
        cancelar.Click += (_, _) => cancelacion?.Cancel();
        exportar.Click += Exportar;
        FormClosing += AlCerrar;

        ConfigurarTabla();
        Controls.Add(CrearContenido());
    }

    private Control CrearContenido()
    {
        var contenido = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
            Padding = new Padding(24, 19, 24, 18)
        };
        contenido.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contenido.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        contenido.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        contenido.Controls.Add(new Label
        {
            Text = "Banco de pruebas de ordenamiento",
            AutoSize = true, Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Azul, Margin = new Padding(0, 0, 0, 14)
        }, 0, 0);

        var controles = new FlowLayoutPanel
        {
            AutoSize = true, WrapContents = true, Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 6)
        };
        controles.Controls.AddRange([
            CrearEtiqueta("Registros"), tamano, CrearEtiqueta("Condición"), condicion,
            CrearEtiqueta("Repeticiones"), repeticiones, ejecutar, cancelar, exportar
        ]);
        contenido.Controls.Add(controles, 0, 1);
        contenido.Controls.Add(tabla, 0, 2);
        contenido.Controls.Add(estado, 0, 3);
        return contenido;
    }

    private void ConfigurarTabla()
    {
        tabla.ColumnHeadersDefaultCellStyle.BackColor = Azul;
        tabla.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        tabla.ColumnHeadersDefaultCellStyle.SelectionBackColor = Azul;
        tabla.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(239, 244, 250);

        AgregarColumna("Método", 130);
        AgregarColumna("Tipo", 70);
        AgregarColumna("Registros", 78);
        AgregarColumna("Condición", 85);
        AgregarColumna("Transcurrido (ms)", 113);
        AgregarColumna("CPU (ms)", 90);
    }

    private void AgregarColumna(string texto, float ancho)
    {
        tabla.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = texto, FillWeight = ancho, SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private async void Ejecutar(object? sender, EventArgs e)
    {
        int[] tamanos = tamano.SelectedIndex == Tamanos.Length
            ? Tamanos : [Tamanos[tamano.SelectedIndex]];
        Condicion[] condiciones = condicion.SelectedIndex == Condiciones.Length
            ? Condiciones : [Condiciones[condicion.SelectedIndex]];
        int veces = (int)repeticiones.Value;
        cancelacion = new CancellationTokenSource();
        var token = cancelacion.Token;

        resultados = [];
        tabla.Rows.Clear();
        CambiarEstado(enCurso: true);
        var progreso = new Progress<string>(mensaje =>
        {
            if (cancelacion is not null && !token.IsCancellationRequested)
                estado.Text = mensaje;
        });

        try
        {
            estado.Text = "Preparando datos y calentando los algoritmos…";
            resultados = await Task.Run(() => BancoPruebas.Ejecutar(
                tamanos, condiciones, veces, progreso, token), token);

            foreach (var fila in resultados)
            {
                tabla.Rows.Add(
                    fila.Metodo, fila.Tipo, fila.Registros.ToString("N0"), fila.Condicion.ToString(),
                    fila.TranscurridoMs.ToString("F3"), fila.CpuMs.ToString("F3"));
            }
            estado.Text = $"{resultados.Count} combinaciones verificadas · Mediana de {veces} repetición(es).";
        }
        catch (OperationCanceledException)
        {
            estado.Text = "Prueba cancelada.";
        }
        catch (Exception error)
        {
            estado.Text = "La ejecución se detuvo.";
            MessageBox.Show(this, error.Message, "Error en la prueba", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            cancelacion.Dispose();
            cancelacion = null;
            CambiarEstado(enCurso: false);
        }
    }

    private void Exportar(object? sender, EventArgs e)
    {
        using var archivo = new SaveFileDialog
        {
            Filter = "Archivo CSV (*.csv)|*.csv", DefaultExt = "csv", FileName = "Resultados_U5.csv"
        };
        if (archivo.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            BancoPruebas.ExportarCsv(archivo.FileName, resultados);
            estado.Text = $"CSV guardado: {Path.GetFileName(archivo.FileName)}.";
        }
        catch (Exception error)
        {
            MessageBox.Show(this, error.Message, "Error al exportar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CambiarEstado(bool enCurso)
    {
        tamano.Enabled = condicion.Enabled = repeticiones.Enabled = ejecutar.Enabled = !enCurso;
        cancelar.Enabled = enCurso;
        exportar.Enabled = !enCurso && resultados.Count > 0;
    }

    private void AlCerrar(object? sender, FormClosingEventArgs e)
    {
        if (cancelacion is null || e.CloseReason != CloseReason.UserClosing)
            return;

        e.Cancel = true;
        cancelacion.Cancel();
        estado.Text = "Cancelando la prueba. Cierra la ventana cuando termine.";
    }

    private static ComboBox CrearSelector() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList, Width = 105,
        Margin = new Padding(4, 4, 17, 2)
    };

    private static Label CrearEtiqueta(string texto) => new()
    {
        Text = texto, AutoSize = true, ForeColor = Azul, Margin = new Padding(0, 7, 0, 2)
    };

    private static Button CrearBoton(string texto, bool principal = false) => new()
    {
        Text = texto, AutoSize = true, Padding = new Padding(6, 1, 6, 1),
        FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false,
        BackColor = principal ? Azul : Color.White, ForeColor = principal ? Color.White : Azul,
        Margin = new Padding(0, 2, 7, 2)
    };
}
