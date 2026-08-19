import { Grid, Box } from '@mui/material'
import ResultsContainer from '../../views/ResultsContainers'

const resultsArray = ['2023', '2022', '2021', '2020', '2019', '2018', '2017', '2016']

const GridContainers: React.FC = () => {
  return (

     <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        m: 2,
        p: 'auto'
      }}
    >
      <Box sx={{ flexGrow: 1 }}>
        <Grid container spacing={2}>
          {resultsArray.map((text: string, index: number) => (
            <ResultsContainer key={index} text={text} />
          ))}
        </Grid>
      </Box>
    </Box>
  )
}

export default GridContainers
