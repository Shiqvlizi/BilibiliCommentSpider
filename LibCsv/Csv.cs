namespace LibCsv
{
    public static class Csv
    {
        public static IEnumerable<TModel> ReadAll<TModel>(string filename)
        {
            using CsvReader<TModel> reader = new CsvReader<TModel>(filename);

            while (reader.Read())
                yield return reader.Current;
        }
        public static IEnumerable<TModel> ReadAll<TModel>(Stream stream)
        {
            using CsvReader<TModel> reader = new CsvReader<TModel>(stream);

            while (reader.Read())
                yield return reader.Current;
        }
        public static IEnumerable<TModel> ReadAll<TModel>(TextReader source)
        {
            using CsvReader<TModel> reader = new CsvReader<TModel>(source);

            while (reader.Read())
                yield return reader.Current;
        }

        public static void WriteAll<TModel>(string filename, IEnumerable<TModel> data)
        {
            using CsvWriter<TModel> writer = new CsvWriter<TModel>(filename);

            foreach (var model in data)
                writer.Write(model);
        }

        public static void WriteAll<TModel>(Stream stream, IEnumerable<TModel> data)
        {
            using CsvWriter<TModel> writer = new CsvWriter<TModel>(stream);

            foreach (var model in data)
                writer.Write(model);
        }

        public static void WriteAll<TModel>(TextWriter dest, IEnumerable<TModel> data)
        {
            using CsvWriter<TModel> writer = new CsvWriter<TModel>(dest);

            foreach (var model in data)
                writer.Write(model);
        }
    }
}