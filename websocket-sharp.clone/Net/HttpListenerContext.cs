/*
 * HttpListenerContext.cs
 *
 * This code is derived from System.Net.HttpListenerContext.cs of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
 * Copyright (c) 2012-2014 sta.blockhead
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

/*
 * Authors:
 * - Gonzalo Paniagua Javier <gonzalo@novell.com>
 */

namespace WebSocketSharp.Net
{
    using System;
    using System.Security.Principal;

    /// <summary>
	/// Provides a set of methods and properties used to access the HTTP request and response
	/// information used by the <see cref="HttpListener"/>.
	/// </summary>
	/// <remarks>
	/// The HttpListenerContext class cannot be inherited.
	/// </remarks>
	internal sealed class HttpListenerContext
    {
        private readonly HttpConnection _connection;
        private string _error;
        private int _errorStatus;
        private readonly HttpListenerRequest _request;
        private readonly HttpListenerResponse _response;
        private IPrincipal _user;

        internal HttpListener Listener;

        internal HttpListenerContext(HttpConnection connection)
        {
            _connection = connection;
            _errorStatus = 400;
            _request = new HttpListenerRequest(this);
            _response = new HttpListenerResponse(this);
        }

        internal HttpConnection Connection => _connection;

        internal string ErrorMessage
        {
            get
            {
                return _error;
            }

            set
            {
                _error = value;
            }
        }

        internal int ErrorStatus
        {
            get
            {
                return _errorStatus;
            }

            set
            {
                _errorStatus = value;
            }
        }

        internal bool HasError => _error != null;

        /// <summary>
		/// Gets the HTTP request information from a client.
		/// </summary>
		/// <value>
		/// A <see cref="HttpListenerRequest"/> that represents the HTTP request.
		/// </value>
		public HttpListenerRequest Request => _request;

        /// <summary>
		/// Gets the HTTP response information used to send to the client.
		/// </summary>
		/// <value>
		/// A <see cref="HttpListenerResponse"/> that represents the HTTP response to send.
		/// </value>
		public HttpListenerResponse Response => _response;

        /// <summary>
		/// Gets the client information (identity, authentication, and security roles).
		/// </summary>
		/// <value>
		/// A <see cref="IPrincipal"/> that represents the client information.
		/// </value>
		public IPrincipal User => _user;

        internal void SetUser(
          AuthenticationSchemes scheme,
          string realm,
          Func<IIdentity, NetworkCredential> credentialsFinder)
        {
            var authRes = AuthenticationResponse.Parse(_request.Headers["Authorization"]);
            if (authRes == null)
                return;

            var id = authRes.ToIdentity();
            if (id == null)
                return;

            NetworkCredential cred = null;
            try
            {
                cred = credentialsFinder(id);
            }
            catch
            {
            }

            if (cred == null)
                return;

            var valid = scheme == AuthenticationSchemes.Basic
                        ? ((HttpBasicIdentity)id).Password == cred.Password
                        : scheme == AuthenticationSchemes.Digest
                          ? ((HttpDigestIdentity)id).IsValid(
                              cred.Password, realm, _request.HttpMethod, null)
                          : false;

            if (valid)
                _user = new GenericPrincipal(id, cred.Roles);
        }
    }
}