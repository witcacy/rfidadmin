import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
    plugins: [react()],
    server: {
        fs: {
            allow: [
                'C:/Users/jmar1947/source/repos/rfidadmin',
                'C:/Users/jmar1947/source/repos/rfidadmin/node_modules',
                // añade más si hace falta
            ]
        }
    }
})