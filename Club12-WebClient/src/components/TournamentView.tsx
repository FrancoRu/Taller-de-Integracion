import { Box, Paper } from '@mui/material'
import Nav from '../layouts/nav'
import DataTable from './DataTable'

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
        <DataTable></DataTable>
        </Paper>
    </>
  )
}

export default TournamentView
