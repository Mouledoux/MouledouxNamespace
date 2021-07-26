using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;


//https://stackoverflow.com/questions/27108264/how-to-properly-make-a-http-web-get-request

namespace Mouledoux.API
{
    public static class NetRequest
    {
        public static string Get(string a_uri)
        {
            HttpWebRequest _request = (HttpWebRequest)WebRequest.Create(a_uri);
            _request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse _response = (HttpWebResponse)_request.GetResponse())
            using (Stream _stream = _response.GetResponseStream())
            using (StreamReader _reader = new StreamReader(_stream))
            {
                string _readerReturn = _reader.ReadToEnd();

                _response.Close();
                _stream.Close();
                _reader.Close();

                return _readerReturn;
            }
        }

        public static async Task<string> GetAsync(string a_uri)
        {
            HttpWebRequest _request = (HttpWebRequest)WebRequest.Create(a_uri);
            _request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse _response = (HttpWebResponse)await _request.GetResponseAsync())
            using (Stream _stream = _response.GetResponseStream())
            using (StreamReader _reader = new StreamReader(_stream))
            {
                Task<string> _readerReturn = _reader.ReadToEndAsync();

                _response.Close();
                _stream.Close();
                _reader.Close();

                return await _readerReturn;
            }
        }



        public static string Post(string a_uri, string a_data, string a_contentType, string a_method = "POST")
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(a_data);

            HttpWebRequest _request = (HttpWebRequest)WebRequest.Create(a_uri);
            _request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            _request.ContentLength = dataBytes.Length;
            _request.ContentType = a_contentType;
            _request.Method = a_method;

            using (Stream _requestBody = _request.GetRequestStream())
            {
                _requestBody.Write(dataBytes, 0, dataBytes.Length);

                _requestBody.Close();
            }

            using (HttpWebResponse _response = (HttpWebResponse)_request.GetResponse())
            using (Stream _stream = _response.GetResponseStream())
            using (StreamReader _reader = new StreamReader(_stream))
            {
                string _readerReturn = _reader.ReadToEnd();

                _response.Close();
                _stream.Close();
                _reader.Close();

                return _readerReturn;
            }
        }


        public static async Task<string> PostAsync(string a_uri, string a_data, string a_contentType, string a_method = "POST")
        {
            byte[] _dataBytes = Encoding.UTF8.GetBytes(a_data);

            HttpWebRequest _request = (HttpWebRequest)WebRequest.Create(a_uri);
            _request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            _request.ContentLength = _dataBytes.Length;
            _request.ContentType = a_contentType;
            _request.Method = a_method;

            using (Stream _requestBody = _request.GetRequestStream())
            {
                await _requestBody.WriteAsync(_dataBytes, 0, _dataBytes.Length);

                _requestBody.Close();
            }

            using (HttpWebResponse _response = (HttpWebResponse)await _request.GetResponseAsync())
            using (Stream _stream = _response.GetResponseStream())
            using (StreamReader _reader = new StreamReader(_stream))
            {
                Task<string> _readerReturn = _reader.ReadToEndAsync();

                _response.Close();
                _stream.Close();
                _reader.Close();

                return await _readerReturn;
            }
        }
    }
}
