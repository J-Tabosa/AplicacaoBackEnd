module.exports = {
  '/api/**': {
    target: process.env.API_TARGET || 'https://localhost:7067',
    secure: false,
    changeOrigin: true,
  },
};
