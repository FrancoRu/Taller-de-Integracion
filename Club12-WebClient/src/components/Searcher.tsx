import React from 'react'
import { Button, Grid, Paper, TextField, Typography } from '@mui/material'

interface SearcherProps {
  text: string
}

const Searcher: React.FC<SearcherProps> = ({ text }) => {
  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    // Add search logic if necessary
  }

  return (
    <Paper
      sx={{
        p: 1,
        mb: 1,
        bgcolor: 'background.paper',
        color: 'text.primary',
        maxWidth: '450px',
        width: '100%',
        margin: 'auto',
        marginTop: '50px'
      }}
    >
      <form onSubmit={handleSubmit}>
        <Grid container spacing={3} justifyContent="center">
          <Grid item xs={2} textAlign="center">
            <Typography>{text}</Typography>
          </Grid>
          <Grid item xs={6} textAlign="center">
            <TextField type="text" id="texto-buscar" variant="outlined" required />
          </Grid>
            <Grid item xs={4} textAlign="center">
              <Button
                type="submit"
                variant="contained"
                sx={{
                  color: 'white', // Set text color to white
                  backgroundColor: 'primary.main' // Set background color using MUI primary color
                }}
                >
                  Buscar
                </Button>
            </Grid>
          </Grid>
      </form>
    </Paper>
  )
}

export default Searcher
