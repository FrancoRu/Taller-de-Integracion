import { Box, Typography } from '@mui/material'
import Searcher from './Searcher'
import GridContainers from './GridContainers'

const Home: React.FC = () => {
  return (
    <>

      <Typography variant="h1" sx={{ p: 4 }}>Club 12 Tournament</Typography>
        <Box sx={{ m: 5 }}>
            <Typography >Pagina oficial de los torneos locales del club 12</Typography>
        </Box>

        <Searcher text='Torneos' />

        <GridContainers></GridContainers>

    </>
  )
}

export default Home
