import { GUID } from '@/modules/core/types/types';
import { useDivision } from '@/modules/division/hook/division.hook';
import { IDivisionResponse } from '@/modules/division/type/division';
import { CustomBox } from '@/views/core/customsThemes/CustomBox';
import { Fixture } from '@/views/division/common/fixture';
import { Positions } from '@/views/division/common/positions';
import { Typography } from '@mui/material';
import React, { useEffect } from 'react';
import { useParams } from 'react-router-dom';

export const DetailDidivion: React.FC = () => {
  const { divisionId } = useParams<{ divisionId: GUID }>();
  const { division, getDivisionsById } = useDivision();

  if (!divisionId) {
    return null;
  }

  useEffect(() => {
    (async () => {
      await getDivisionsById(divisionId);
    })();
  }, []);

  if (!division) {
    return null;
  }

  return <RenderContent {...division} />;
};

const NoStagesMessage: React.FC = () => (
  <CustomBox>
    <Typography>
      No se encontraron fechas cargadas para esta division todavía
    </Typography>
  </CustomBox>
);

const RenderContent: React.FC<IDivisionResponse> = ({ stages }) => {
  if (!stages || stages.length === 0) {
    return <NoStagesMessage />;
  }
  return (
    <>
      <Fixture stages={stages} />
      <Positions stages={stages} />
    </>
  );
};
