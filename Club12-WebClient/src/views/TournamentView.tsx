import { Paper } from '@mui/material'
import Nav from './layouts/nav'

const TournamentView: React.FC = () => {
  return (
        <>
        <Nav/>

        <Paper
        sx={{
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          m: 2,
          p: 'auto'
        }}
        >
        </Paper>
    </>
  )
}

export default TournamentView
