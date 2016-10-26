using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace SoftwareSecurity
{
	public enum ProviderType
	{
		Default
	}

	public class CryptographyRSA
	{
		const int ENCRYPTED_LENGTH = 128;
		const int MAX_BYTES_TO_ENCRYPT = 117;

		int PROVIDER_RSA_FULL = 1;
		RSACryptoServiceProvider rsa;
		public RSACryptoServiceProvider RSA { get { return rsa; } }

		string publicPrivateKeyXML;
		string publicOnlyKeyXML;

		public string PublicOnlyKeyXML
		{
			get { return publicOnlyKeyXML; }
		}

		public string PublicPrivateKeyXML
		{
			get { return PublicPrivateKeyXML; }
		}

		public CryptographyRSA(ProviderType providerType, string password)
		{
			switch (providerType)
			{
				case ProviderType.Default: PROVIDER_RSA_FULL = 1; break;
				default: throw new Exception("Unsupported provider");
			}

			CspParameters cspParams;
			cspParams = new CspParameters(PROVIDER_RSA_FULL);
			cspParams.KeyContainerName = password;
			cspParams.Flags = CspProviderFlags.UseDefaultKeyContainer;
			
			cspParams.ProviderName = "Microsoft Strong Cryptographic Provider";
			rsa = new RSACryptoServiceProvider(cspParams);

			//provide public and private RSA params
			publicPrivateKeyXML = rsa.ToXmlString(true);

			//provide public only RSA params
			publicOnlyKeyXML = rsa.ToXmlString(false);
		}

		public CryptographyRSA(string publicOnlyKeyXML)
		{
			this.publicOnlyKeyXML = publicOnlyKeyXML;
			rsa = new RSACryptoServiceProvider();
		}

		public string EncryptData(string data2Encrypt)
		{		
			byte[] plainbytes = System.Text.Encoding.UTF8.GetBytes(data2Encrypt);
			byte[] cipherbytes = EncryptData(plainbytes);
			return Convert.ToBase64String(cipherbytes);
		}

		public byte[] EncryptData(byte[] data2Encrypt)
		{
			rsa.FromXmlString(publicOnlyKeyXML);
			if (data2Encrypt.Length <= MAX_BYTES_TO_ENCRYPT)
				return rsa.Encrypt(data2Encrypt, false);

			int dataLength = ENCRYPTED_LENGTH + ENCRYPTED_LENGTH * ((data2Encrypt.Length - 1) / MAX_BYTES_TO_ENCRYPT);
			byte[] data = new byte[dataLength];
			int dataIndex = 0;

			for (int i = 0; i < data2Encrypt.Length; i += MAX_BYTES_TO_ENCRYPT)
			{
				int length = data2Encrypt.Length - i;
				if (length > MAX_BYTES_TO_ENCRYPT)
					length = MAX_BYTES_TO_ENCRYPT;

				byte[] block = new byte[length];
				Array.Copy(data2Encrypt, i, block, 0, length);

				byte[] encrypted = rsa.Encrypt(block, false);
				Array.Copy(encrypted, 0, data, dataIndex, ENCRYPTED_LENGTH);
				dataIndex += ENCRYPTED_LENGTH;
			}

			return data;
		}

		public string DecryptData(string data2Decrypt)
		{
			if (publicPrivateKeyXML == null)
				return null;

			byte[] plainbytes = Convert.FromBase64String(data2Decrypt);
			byte[] plain = DecryptData(plainbytes);
			return System.Text.Encoding.UTF8.GetString(plain);
		}

		public byte[] DecryptData(byte[] data2Decrypt)
		{
			if (publicPrivateKeyXML == null)
				return null;

			rsa.FromXmlString(publicPrivateKeyXML);
			if (data2Decrypt.Length <= ENCRYPTED_LENGTH)
				return rsa.Decrypt(data2Decrypt, false);

			List<byte> data = new List<byte>();

			for (int i = 0; i < data2Decrypt.Length; i += ENCRYPTED_LENGTH)
			{
				byte[] block = new byte[ENCRYPTED_LENGTH];
				Array.Copy(data2Decrypt, i, block, 0, ENCRYPTED_LENGTH);

				byte[] decrypted = rsa.Decrypt(block, false);
				data.AddRange(decrypted);
			}

			return data.ToArray();
		}
	}
}
