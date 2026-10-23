"""Call Unity Pipeline MCP: python Tools/unity_mcp.py TOOL JSON_ARGUMENTS.

Use --list to inspect tools. The transport is Unity CLI's stdio MCP server.
"""
import json
import os
import queue
import subprocess
import sys
import threading


def main():
    executable = os.path.join(os.environ['LOCALAPPDATA'], 'Unity', 'bin', 'unity.exe')
    project = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    process = subprocess.Popen(
        [executable, 'mcp', '--project-path', project, '--proxy-disable'],
        stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        universal_newlines=True, encoding='utf-8', bufsize=1)
    responses = queue.Queue()

    def receive():
        for line in process.stdout:
            try:
                responses.put(json.loads(line))
            except ValueError:
                pass

    threading.Thread(target=receive, daemon=True).start()

    def send(message):
        process.stdin.write(json.dumps(message) + '\n')
        process.stdin.flush()

    def request(identifier, method, parameters):
        send(dict(jsonrpc='2.0', id=identifier, method=method, params=parameters))
        while True:
            response = responses.get(timeout=90)
            if response.get('id') == identifier:
                return response

    try:
        request(1, 'initialize', dict(protocolVersion='2024-11-05', capabilities={},
                                     clientInfo=dict(name='Codex', version='1.0')))
        send(dict(jsonrpc='2.0', method='notifications/initialized'))
        if sys.argv[1] == '--list':
            result = request(2, 'tools/list', {})
        else:
            result = request(2, 'tools/call', dict(
                name=sys.argv[1], arguments=json.loads(sys.argv[2]) if len(sys.argv) > 2 else {}))
        print(json.dumps(result, ensure_ascii=True))
    finally:
        process.terminate()


if __name__ == '__main__':
    main()
