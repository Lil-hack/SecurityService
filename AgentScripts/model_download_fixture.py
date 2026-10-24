from http.server import ThreadingHTTPServer, BaseHTTPRequestHandler
import time, hashlib, json, ssl, datetime, ipaddress
from cryptography import x509
from cryptography.x509.oid import NameOID
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import rsa
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'.local'/'MenuQA'
root.mkdir(parents=True,exist_ok=True)
payload=b'LITERTLM'+bytes(range(256))*16384
(root/'fixture.bin').write_bytes(payload)
key=rsa.generate_private_key(public_exponent=65537,key_size=2048)
name=x509.Name([x509.NameAttribute(NameOID.COMMON_NAME,'Local menu download fixture')])
now=datetime.datetime.utcnow()
cert=x509.CertificateBuilder().subject_name(name).issuer_name(name).public_key(key.public_key()).serial_number(x509.random_serial_number()).not_valid_before(now-datetime.timedelta(minutes=5)).not_valid_after(now+datetime.timedelta(days=2)).add_extension(x509.SubjectAlternativeName([x509.IPAddress(ipaddress.ip_address('127.0.0.1'))]),critical=False).sign(key,hashes.SHA256())
(root/'cert.pem').write_bytes(cert.public_bytes(serialization.Encoding.PEM))
(root/'key.pem').write_bytes(key.private_bytes(serialization.Encoding.PEM,serialization.PrivateFormat.PKCS8,serialization.NoEncryption()))
(root/'spec.json').write_text(json.dumps({'bytes':len(payload),'sha256':hashlib.sha256(payload).hexdigest(),'certSha256':cert.fingerprint(hashes.SHA256()).hex()}))
class Handler(BaseHTTPRequestHandler):
 def do_GET(self):
  if self.path=='/missing': self.send_error(404);return
  start=int(self.headers.get('Range','bytes=0-').split('=')[1].split('-')[0])
  if self.path=='/ignore':start=0
  data=payload[start:]
  self.send_response(200 if self.path=='/ignore' else 206)
  self.send_header('Content-Length',str(len(data)))
  if self.path!='/ignore':self.send_header('Content-Range',f'bytes {start}-{len(payload)-1}/{len(payload)}')
  self.end_headers()
  try:
   for pos in range(0,len(data),65536):self.wfile.write(data[pos:pos+65536]);self.wfile.flush();time.sleep(.025)
  except (BrokenPipeError,ConnectionResetError):pass
  with (root/'requests.log').open('a') as f:f.write(f'{self.path} offset={start}\n')
 def log_message(self,*args):pass
server=ThreadingHTTPServer(('127.0.0.1',18766),Handler)
context=ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
context.load_cert_chain(str(root/'cert.pem'),str(root/'key.pem'))
server.socket=context.wrap_socket(server.socket,server_side=True)
server.serve_forever()
