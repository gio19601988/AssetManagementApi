// Exceptions/NotFoundException.cs
using System;

namespace AssetManagementApi.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}