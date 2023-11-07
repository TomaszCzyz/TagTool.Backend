import grpc from 'k6/net/grpc';
import { check, sleep } from 'k6';

export const options = {
  vus: 1,
  duration: '10s',
};

const client = new grpc.Client();
client.load([
    'src/TagTool.Backend.Contracts/Protos/',
    'src/TagTool.Backend.Contracts/'
  ],
  'TagService.proto');

export default function () {
  client.connect('127.0.0.1:5280', {
    plaintext: true
  });

  const req = {
    id: 'B9923340-612E-4075-A2D6-4CA8ECF3E5B8'
  };
  const response = client.invoke('TagToolBackend.TagService/GetItem', req);

  check(response, {
    'status is OK': (r) => r && r.status === grpc.StatusOK,
  });

  // console.log(JSON.stringify(response.message));

  client.close();
}
