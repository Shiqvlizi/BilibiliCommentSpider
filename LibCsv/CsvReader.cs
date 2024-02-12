using LibStringNaming;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace LibCsv
{
    public class CsvReader<TModel> : IDisposable
    {
        private static Type _modelType = typeof(TModel);

        private bool _closeBaseStreamWhileDisposing = false;
        private TModel? _current;
        private PropertyInfo[]? _headers;
        private int _rowSkip = -1;
        private int _columnSkip = -1;
        private int _rowTake = -1;
        private int _columnTake = -1;

        private int _rowSkipped = 0;
        private int _rowTaken = 0;

        public CsvReader(Stream stream)
        {
            EnsureModelOk();

            BaseStream = stream;
            TextReader = new StreamReader(stream);

            _closeBaseStreamWhileDisposing = false;
        }

        public CsvReader(TextReader reader)
        {
            EnsureModelOk();

            TextReader = reader;

            if (reader is StreamReader streamReader)
                BaseStream = streamReader.BaseStream;

            _closeBaseStreamWhileDisposing = false;
        }

        public CsvReader(string filename)
        {
            EnsureModelOk();

            BaseStream = File.OpenRead(filename);
            TextReader = new StreamReader(BaseStream, leaveOpen: true);

            _closeBaseStreamWhileDisposing = true;
        }

        ~CsvReader()
        {
            Dispose(false);
        }

        public Stream? BaseStream { get; }
        public TextReader TextReader { get; }

        public int RowSkip { get => _rowSkip; set => SetInitProperty(ref _rowSkip, value); }
        public int ColumnSkip { get => _columnSkip; set => SetInitProperty(ref _columnSkip, value); }
        public int RowTake { get => _rowTake; set => SetInitProperty(ref _rowTake, value); }
        public int ColumnTake { get => _columnTake; set => SetInitProperty(ref _columnTake, value); }

        public TModel Current => _current ?? throw new InvalidOperationException("Please call 'Read' before get value of 'Current'");

        private void EnsureModelOk()
        {
            if (_modelType.GetConstructor(Array.Empty<Type>()) is not ConstructorInfo)
                throw new ArgumentException($"Model must have a public parameterless constructor", nameof(TModel));
        }

        private void SetInitProperty<T>(ref T field, T value)
        {
            if (_headers != null)
                throw new InvalidOperationException("Property can be set only before calling 'Read'");

            field = value;
        }

        [MemberNotNull(nameof(_headers))]
        private void ReadHeaders()
        {
            string? headerText = TextReader.ReadLine();
            if (headerText == null)
            {
                _headers = Array.Empty<PropertyInfo>();
                return;
            }

            CsvCell[] headerCells = CsvHelper.SplitData(headerText);
            _headers = new PropertyInfo[headerCells.Length];

            for (int i = 0; i < _headers.Length; i++)
            {
                string header = headerCells[i].Data;
                PropertyInfo? prop = (_modelType.GetProperty(header)
                    ?? _modelType.GetProperty(StringUtils.ToPascal(header)))
                    ?? throw new InvalidOperationException($"Can't find proeprty for csv header '{header}'");

                _headers[i] = prop;
            }
        }

        private void PopulateToModel(TModel model, CsvCell[] cells)
        {
            int skip = ColumnSkip;
            if (skip == -1)
                skip = 0;
            int take = ColumnTake;
            if (take == -1)
                take = cells.Length - skip;

            for (int i = 0; i < take; i++)
            {
                int index = skip + i;
                CsvCell cell = cells[index];
                PropertyInfo property = _headers![index];
                Type targetType = property.PropertyType;

                if (index >= cells.Length || index >= _headers.Length)
                    break;

                if (targetType.IsEnum)
                {
                    if (string.IsNullOrWhiteSpace(cell.Data))
                        continue;

                    if (Enum.TryParse(targetType, cell.Data, out object? enumValue))
                        property.SetValue(model, enumValue);
                }
                else if (targetType.IsAssignableTo(typeof(IConvertible)))
                {
                    try
                    {
                        IConvertible convertible = cell.Data;
                        property.SetValue(model, convertible.ToType(targetType, null));
                    }
                    catch { }
                }
                else
                {

                }
            }
        }

        public bool Read()
        {
            if (_headers == null)
                ReadHeaders();

            while (RowSkip != -1 && _rowSkipped < RowSkip)
            {
                TextReader.ReadLine();
                _rowSkipped++;
            }

            if (RowTake != -1 && RowTake == _rowTaken)
            {
                return false;
            }

            string? line = TextReader.ReadLine();

            if (line == null)
                return false;

            CsvCell[] cells = CsvHelper.SplitData(line);
            TModel model = Activator.CreateInstance<TModel>();

            PopulateToModel(model, cells);

            _current = model;
            return true;
        }





        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);

            if (_closeBaseStreamWhileDisposing)
                BaseStream?.Dispose();
        }
    }
}