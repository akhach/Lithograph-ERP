import { createTheme } from '@mui/material/styles'

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: '#1f4e79' },
    secondary: { main: '#5a6b7b' },
    background: { default: '#f5f6f8', paper: '#ffffff' },
  },
  typography: {
    fontFamily: ['"Segoe UI"', 'Roboto', '"Helvetica Neue"', 'Arial', 'sans-serif'].join(','),
    fontSize: 14,
    button: { textTransform: 'none' },
  },
  shape: { borderRadius: 6 },
  spacing: 8,
  components: {
    MuiButton: { defaultProps: { disableElevation: true } },
    MuiAppBar: { defaultProps: { elevation: 0 } },
    MuiTextField: { defaultProps: { size: 'small' } },
  },
})
