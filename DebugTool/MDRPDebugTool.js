const http = require('http');

const requestListener = function (req, res) {
  req.on('data', chunk => {
    console.log(Date().toString(), decodeURI(chunk.toString()));
  });
  res.end('Logged');
}

const server = http.createServer(requestListener);
server.listen(7532);