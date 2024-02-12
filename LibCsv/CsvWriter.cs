using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace LibCsv
{
    public class CsvWriter<TModel> : IDisposable
    {
        private static Type _modelType = typeof(TModel);
        private PropertyInfo[]? _headers;

        private bool _closeBaseStreamWhileDisposing = false;

        public CsvWriter(Stream stream)
        {
            BaseStream = stream;
            TextWriter = new StreamWriter(stream, leaveOpen: true);

            _closeBaseStreamWhileDisposing = false;
        }

        public CsvWriter(TextWriter writer)
        {
            TextWriter = writer;

            if (writer is StreamWriter streamWriter)
                BaseStream = streamWriter.BaseStream;

            _closeBaseStreamWhileDisposing = false;
        }

        public CsvWriter(string filename)
        {
            BaseStream = File.Create(filename);
            TextWriter = new StreamWriter(BaseStream, leaveOpen: true);

            _closeBaseStreamWhileDisposing = true;
        }

        ~CsvWriter()
        {
            Dispose(false);
        }

        public Stream? BaseStream { get; }
        public TextWriter TextWriter { get; }


        [MemberNotNull(nameof(_headers))]
        private void GenerateHeaders()
        {
            _headers = _modelType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty);
        }

        private void WriteHeaders()
        {
            string headersText = string.Join(",", _headers!.Select(p => p.Name));
            TextWriter.WriteLine(headersText);
        }

        public void Write(TModel data)
        {
            if (_headers == null)
            {
                GenerateHeaders();
                WriteHeaders();
            }

            string rowText = string.Join(",", _headers.Select(p => CsvHelper.FormatData(p.GetValue(data)?.ToString() ?? string.Empty)));
            TextWriter.WriteLine(rowText);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
                GC.SuppressFinalize(this);

            TextWriter.Flush();
            BaseStream?.Flush();
            if (_closeBaseStreamWhileDisposing)
                BaseStream?.Dispose();
        }
    }
}