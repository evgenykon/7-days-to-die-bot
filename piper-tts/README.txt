Piper TTS Server for 7D2D Companion Bot
=========================================

1. Download piper.exe from https://github.com/rhasspy/piper/releases
   - Get the Windows binary (piper_windows_amd64.zip or similar)
   - Extract piper.exe into this folder

2. Download a voice model (.onnx file + .json config) from:
   https://huggingface.co/rhasspy/piper-voices/tree/main/
   - Place voice.onnx and voice.onnx.json in this folder

3. Run: run.bat
   - Starts PiperServer.exe on port 9092

4. Stop: stop.bat

Bot-server (Docker) sends text to http://host.docker.internal:9092/speak
