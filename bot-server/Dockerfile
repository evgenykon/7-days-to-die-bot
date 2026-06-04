FROM node:22-alpine
RUN mkdir -p /data
WORKDIR /app
COPY package.json ./
RUN npm install
COPY *.js ./
COPY lib/ ./lib/
EXPOSE 9091
CMD ["node", "server.js"]
