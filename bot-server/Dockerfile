FROM node:22-alpine
WORKDIR /app
COPY package.json ./
RUN npm install
COPY *.js ./
EXPOSE 9091
CMD ["node", "server.js"]
