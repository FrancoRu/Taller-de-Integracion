import React from 'react'
import { Button, Grid, Paper, TextField, Typography } from '@mui/material'

interface SearcherProps {
  text: string
}

const Searcher: React.FC<SearcherProps> = ({ text }) => {
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    // Agrega lógica de búsqueda si es necesario
  }

  return (
    <Paper
      sx={{
        p: 1,
        mb: 1,
        bgcolor: 'background.paper',
        color: 'text.primary',
        maxWidth: '450px',
        width: '100%'
      }}>
      <form onSubmit={handleSubmit}>
      <Grid container spacing={3}>
      <Grid item xs={2} >
            <Typography >
            {text}
            </Typography>
        </Grid>
        <Grid item xs={6} >
            <TextField
                type="text"
                id="texto-buscar"
                variant="outlined"
                required
            />
          </Grid>
          <Grid item xs={4}>
            <Button type="submit" variant="contained" color="primary">
                Buscar
            </Button>
          </Grid>
        </Grid>
      </form>
    </Paper>
  )
}

export default Searcher
